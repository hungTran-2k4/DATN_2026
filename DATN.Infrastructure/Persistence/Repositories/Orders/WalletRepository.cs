using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

public class WalletRepository : IWalletRepository
{
    private readonly DataAccessAdapter _adapter;

    public WalletRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<decimal> GetAvailableBalanceAsync(Guid shopId, CancellationToken ct = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Select(ShopFields.AvailableBalance).Where(ShopFields.Id == shopId);
        return await _adapter.FetchScalarAsync<decimal>(query, ct);
    }

    public async Task<decimal> GetLockedBalanceAsync(Guid shopId, CancellationToken ct = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Select(ShopFields.LockedBalance).Where(ShopFields.Id == shopId);
        return await _adapter.FetchScalarAsync<decimal>(query, ct);
    }

    public async Task<bool> UpdateBalanceAsync(Guid shopId, decimal amount, string type, string description, Guid? transactionId = null, CancellationToken ct = default)
    {
        try
        {
            await _adapter.StartTransactionAsync(System.Data.IsolationLevel.ReadCommitted, "UpdateBalance", ct);

            var qf = new QueryFactory();
            var query = qf.Shop.Where(ShopFields.Id == shopId);
            var shop = await _adapter.FetchFirstAsync(query, ct);

            if (shop == null) 
            {
                _adapter.Rollback();
                return false;
            }

            bool isLocked = type.ToUpper() == "LOCKED";
            decimal balanceBefore = isLocked ? (shop.LockedBalance ?? 0) : (shop.AvailableBalance ?? 0);
            decimal newBalance = balanceBefore + amount;

            if (isLocked)
                shop.LockedBalance = newBalance;
            else
                shop.AvailableBalance = newBalance;

            shop.IsNew = false;
            await _adapter.SaveEntityAsync(shop, true, ct);

            // Ghi Ledger
            var ledger = new WalletLedgerEntity
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                TransactionId = transactionId,
                Amount = amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = newBalance,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsNew = true
            };
            await _adapter.SaveEntityAsync(ledger, true, ct);

            _adapter.Commit();
            return true;
        }
        catch
        {
            if (_adapter.IsTransactionInProgress) _adapter.Rollback();
            throw;
        }
    }

    public async Task<bool> ReleaseLockedFundsAsync(Guid shopId, decimal amount, string description, CancellationToken ct = default)
    {
        try
        {
            await _adapter.StartTransactionAsync(System.Data.IsolationLevel.ReadCommitted, "ReleaseFunds", ct);

            var qf = new QueryFactory();
            var query = qf.Shop.Where(ShopFields.Id == shopId);
            var shop = await _adapter.FetchFirstAsync(query, ct);
            
            if (shop == null)
            {
                _adapter.Rollback();
                return false;
            }

            decimal currentLocked = shop.LockedBalance ?? 0;
            decimal currentAvailable = shop.AvailableBalance ?? 0;

            if (currentLocked < amount) amount = currentLocked; // Tránh âm

            shop.LockedBalance = currentLocked - amount;
            shop.AvailableBalance = currentAvailable + amount;
            shop.IsNew = false;
            await _adapter.SaveEntityAsync(shop, true, ct);

            // Ghi Ledger cho phần tăng Available
            var ledger = new WalletLedgerEntity
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Amount = amount,
                BalanceBefore = currentAvailable,
                BalanceAfter = shop.AvailableBalance ?? 0,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsNew = true
            };
            await _adapter.SaveEntityAsync(ledger, true, ct);

            _adapter.Commit();
            return true;
        }
        catch
        {
            if (_adapter.IsTransactionInProgress) _adapter.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<WalletLedger>> GetLedgersAsync(Guid shopId, int limit = 50, CancellationToken ct = default)
    {
        var qf = new QueryFactory();
        var query = qf.WalletLedger
            .Where(WalletLedgerFields.ShopId == shopId)
            .OrderBy(WalletLedgerFields.CreatedAt.Descending())
            .Limit(limit);

        var entities = await _adapter.FetchQueryAsync(query, ct);
        return entities.Cast<WalletLedgerEntity>().Select(e => new WalletLedger
        {
            Id = e.Id,
            ShopId = e.ShopId,
            TransactionId = e.TransactionId,
            Amount = e.Amount,
            BalanceBefore = e.BalanceBefore,
            BalanceAfter = e.BalanceAfter,
            Description = e.Description,
            CreatedAt = e.CreatedAt ?? DateTime.UtcNow
        });
    }

    public async Task ProcessEscrowReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            var qf = new QueryFactory();
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            // 1. Tìm các bản ghi Ký quỹ đã quá 7 ngày
            var query = qf.WalletLedger
                .Where(WalletLedgerFields.Description.Contains("Ký quỹ 7 ngày"))
                .AndWhere(WalletLedgerFields.CreatedAt <= sevenDaysAgo);

            var matureLedgers = await _adapter.FetchQueryAsync(query, ct);

            foreach (var ledger in matureLedgers.Cast<WalletLedgerEntity>())
            {
                // 2. Kiểm tra xem đã giải phóng chưa (tránh trùng lặp)
                if (!ledger.TransactionId.HasValue) continue;

                var checkQuery = qf.WalletLedger
                    .Where(WalletLedgerFields.TransactionId == ledger.TransactionId.Value)
                    .AndWhere(WalletLedgerFields.Description.Contains("Giải phóng ký quỹ"));

                var existingRelease = await _adapter.FetchFirstAsync(checkQuery, ct);
                if (existingRelease != null) continue; // Đã giải phóng rồi

                // 3. Thực hiện giải phóng
                var orderCodeMatch = System.Text.RegularExpressions.Regex.Match(ledger.Description, @"ORD-[A-Z0-9]+");
                var orderCode = orderCodeMatch.Success ? orderCodeMatch.Value : "Unknown";
                var description = $"Giải phóng ký quỹ đơn hàng {orderCode}";
                
                await ReleaseLockedFundsForShopAsync(ledger.ShopId, ledger.Amount, description, ledger.TransactionId, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessEscrowReleaseAsync: {ex.Message}");
        }
    }

    private async Task ReleaseLockedFundsForShopAsync(Guid shopId, decimal amount, string description, Guid? transactionId, CancellationToken ct)
    {
        try
        {
            await _adapter.StartTransactionAsync(System.Data.IsolationLevel.ReadCommitted, "ReleaseEscrow", ct);

            var qf = new QueryFactory();
            var query = qf.Shop.Where(ShopFields.Id == shopId);
            var shop = await _adapter.FetchFirstAsync(query, ct);

            if (shop != null)
            {
                decimal availableBefore = shop.AvailableBalance ?? 0;
                decimal lockedBefore = shop.LockedBalance ?? 0;

                shop.AvailableBalance = availableBefore + amount;
                shop.LockedBalance = lockedBefore - amount;
                shop.IsNew = false;
                await _adapter.SaveEntityAsync(shop, true, ct);

                // Ghi sổ cái
                var ledger = new WalletLedgerEntity
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    TransactionId = transactionId,
                    Amount = amount,
                    BalanceBefore = availableBefore,
                    BalanceAfter = shop.AvailableBalance ?? 0,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    IsNew = true
                };
                await _adapter.SaveEntityAsync(ledger, true, ct);
            }

            await _adapter.CommitAsync(ct);
        }
        catch
        {
            if (_adapter.IsTransactionInProgress) _adapter.Rollback();
            throw;
        }
    }
    public async Task<bool> RefundLockedFundsAsync(Guid shopId, decimal amount, string description, CancellationToken ct = default)
    {
        try
        {
            _adapter.StartTransaction(System.Data.IsolationLevel.ReadCommitted, "RefundLockedFunds");
            
            var qf = new QueryFactory();
            var query = qf.Shop.Where(ShopFields.Id == shopId);
            var shop = await _adapter.FetchFirstAsync(query, ct);

            if (shop == null)
            {
                _adapter.Rollback();
                return false;
            }

            decimal currentLocked = shop.LockedBalance ?? 0;
            if (currentLocked < amount) amount = currentLocked;

            shop.LockedBalance = currentLocked - amount;
            shop.IsNew = false;
            await _adapter.SaveEntityAsync(shop, true, ct);

            var ledger = new WalletLedgerEntity
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Amount = 0, // Không đổi Available
                BalanceBefore = shop.AvailableBalance ?? 0,
                BalanceAfter = shop.AvailableBalance ?? 0,
                Description = "REFUND_LOCKED: " + description,
                CreatedAt = DateTime.UtcNow,
                IsNew = true
            };
            await _adapter.SaveEntityAsync(ledger, true, ct);

            _adapter.Commit();
            return true;
        }
        catch
        {
            if (_adapter.IsTransactionInProgress) _adapter.Rollback();
            throw;
        }
    }
}
