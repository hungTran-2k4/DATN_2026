using DATN.Application.DTOs.Statistics;
using DATN.Application.Interfaces.Services;
using DATN_2026.DatabaseSpecific;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly DataAccessAdapter _adapter;

    public StatisticsService(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<AdminDashboardStatsDto> GetAdminDashboardStats()
    {
        var qf = new QueryFactory();
        var stats = new AdminDashboardStatsDto();

        // 1. Total Counts
        stats.TotalUsers = await _adapter.FetchScalarAsync<int>(qf.User.Select(Functions.CountRow()));
        stats.TotalShops = await _adapter.FetchScalarAsync<int>(qf.Shop.Select(Functions.CountRow()));
        stats.TotalProducts = await _adapter.FetchScalarAsync<int>(qf.Product.Select(Functions.CountRow()));

        // 2. Total System Revenue & Commission (Admin Revenue)
        var revenueQuery = qf.Order
            .Where(OrderFields.OrderStatus == "DELIVERED" | OrderFields.OrderStatus == "COMPLETED")
            .Select(OrderFields.TotalAmount.Sum(), OrderFields.CommissionFee.Sum());
        var revenueRows = await _adapter.FetchQueryAsync(revenueQuery);
        var revenueResult = revenueRows.FirstOrDefault() as object[];
        
        var totalAmount = revenueResult?[0] as decimal? ?? 0;
        var totalCommission = revenueResult?[1] as decimal? ?? 0;

        stats.TotalRevenue = totalCommission; // Admin revenue is the commission
        stats.TotalSales = totalAmount; // Optional: can add this to DTO if needed

        // 3. Monthly Revenue (Last 6 months)
        var last6Months = DateTime.UtcNow.AddMonths(-6);
        var monthlyQuery = qf.Order
            .Where(OrderFields.OrderStatus == "COMPLETED" & OrderFields.CreatedAt >= last6Months)
            .Select(OrderFields.TotalAmount, OrderFields.CreatedAt);
        
        var orders = await _adapter.FetchQueryAsync(monthlyQuery);
        stats.MonthlyRevenue = orders.Cast<object[]>()
            .Select(r => new { Date = (DateTime)r[1], Amount = (decimal)r[0] })
            .GroupBy(x => x.Date.ToString("yyyy-MM"))
            .Select(g => new MonthlyRevenueDto { Month = g.Key, Revenue = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Month)
            .ToList();

        // 4. Order Status Distribution
        var statusQuery = qf.Order
            .Select(OrderFields.OrderStatus, Functions.CountRow())
            .GroupBy(OrderFields.OrderStatus);
        var statusRows = await _adapter.FetchQueryAsync(statusQuery);
        stats.OrderStatusDistribution = statusRows.Cast<object[]>()
            .Select(r => new OrderStatusDistributionDto { Status = r[0].ToString()!, Count = Convert.ToInt32(r[1]) })
            .ToList();

        // 5. Top Shops
        var topShopsQuery = qf.Create()
            .From(qf.Order
                .InnerJoin(qf.OrderItem).On(OrderFields.Id == OrderItemFields.OrderId)
                .InnerJoin(qf.ProductVariant).On(OrderItemFields.VariantId == ProductVariantFields.Id)
                .InnerJoin(qf.Product).On(ProductVariantFields.ProductId == ProductFields.Id)
                .InnerJoin(qf.Shop).On(ProductFields.ShopId == ShopFields.Id))
            .Where(OrderFields.OrderStatus == "COMPLETED")
            .Select(ShopFields.Name, OrderFields.TotalAmount.Sum())
            .GroupBy(ShopFields.Name)
            .OrderBy(OrderFields.TotalAmount.Sum().Descending())
            .Limit(5);
        var topShopsRows = await _adapter.FetchQueryAsync(topShopsQuery);
        stats.TopShops = topShopsRows.Cast<object[]>()
            .Select(r => new TopShopDto { ShopName = r[0].ToString()!, Revenue = (decimal)r[1] })
            .ToList();

        // [DEMO] Add fake data if empty
        if (stats.TotalRevenue == 0)
        {
            ApplyAdminDemoData(stats);
        }

        return stats;
    }

    private void ApplyAdminDemoData(AdminDashboardStatsDto stats)
    {
        // Realistic counts
        if (stats.TotalUsers < 10) stats.TotalUsers = 1248;
        if (stats.TotalShops < 5) stats.TotalShops = 85;
        if (stats.TotalProducts < 20) stats.TotalProducts = 5420;
        stats.TotalRevenue = 1254000000; // 1.25B VND

        // 6 months revenue trend
        var now = DateTime.UtcNow;
        stats.MonthlyRevenue = Enumerable.Range(0, 6).Select(i => {
            var date = now.AddMonths(-5 + i);
            return new MonthlyRevenueDto {
                Month = date.ToString("yyyy-MM"),
                Revenue = 150000000 + (new Random().Next(50, 200) * 1000000)
            };
        }).ToList();

        // Status distribution
        stats.OrderStatusDistribution = new List<OrderStatusDistributionDto> {
            new() { Status = "PENDING", Count = 45 },
            new() { Status = "PROCESSING", Count = 120 },
            new() { Status = "COMPLETED", Count = 850 },
            new() { Status = "CANCELLED", Count = 32 }
        };

        // Top Shops
        stats.TopShops = new List<TopShopDto> {
            new() { ShopName = "Apple Flagship Store", Revenue = 450000000 },
            new() { ShopName = "Samsung Official", Revenue = 320000000 },
            new() { ShopName = "Sony Vietnam", Revenue = 210000000 },
            new() { ShopName = "Anker Store", Revenue = 150000000 },
            new() { ShopName = "Logitech G", Revenue = 124000000 }
        };
    }

    public async Task<SellerDashboardStatsDto> GetSellerDashboardStats(Guid shopId)
    {
        var qf = new QueryFactory();
        var stats = new SellerDashboardStatsDto();

        // Join to filter by ShopId
        var shopOrderJoin = qf.Order
            .InnerJoin(qf.OrderItem).On(OrderFields.Id == OrderItemFields.OrderId)
            .InnerJoin(qf.ProductVariant).On(OrderItemFields.VariantId == ProductVariantFields.Id)
            .InnerJoin(qf.Product).On(ProductVariantFields.ProductId == ProductFields.Id);

        // 1. Total Orders (Directly using ShopId)
        var totalOrdersQuery = qf.Order.Where(OrderFields.ShopId == shopId).Select(Functions.CountRow());
        stats.TotalOrders = await _adapter.FetchScalarAsync<int>(totalOrdersQuery);


        // 2. Total Revenue (Net = Total - Commission)
        var revenueQuery = qf.Order
            .Where(OrderFields.ShopId == shopId & (OrderFields.OrderStatus == "DELIVERED" | OrderFields.OrderStatus == "COMPLETED"))
            .Select(OrderFields.TotalAmount.Sum(), OrderFields.CommissionFee.Sum());
        var revRows = await _adapter.FetchQueryAsync(revenueQuery);
        var revResult = revRows.FirstOrDefault() as object[];
        
        var rawTotal = revResult?[0] as decimal? ?? 0;
        var rawComm = revResult?[1] as decimal? ?? 0;
        stats.TotalRevenue = rawTotal - rawComm;

        // 2b. Balances from Shop table
        var balanceQuery = qf.Shop
            .Where(ShopFields.Id == shopId)
            .Select(ShopFields.AvailableBalance, ShopFields.LockedBalance);
        var balanceRows = await _adapter.FetchQueryAsync(balanceQuery);
        var balanceResult = balanceRows.FirstOrDefault() as object[];
        stats.AvailableBalance = balanceResult?[0] as decimal? ?? 0;
        stats.LockedBalance = balanceResult?[1] as decimal? ?? 0;

        // 3. Total Products
        stats.TotalProducts = await _adapter.FetchScalarAsync<int>(qf.Product.Where(ProductFields.ShopId == shopId).Select(Functions.CountRow()));

        // 4. Daily Revenue (Last 30 days)
        var last30Days = DateTime.UtcNow.AddDays(-30);
        var dailyQuery = qf.Create()
            .From(shopOrderJoin)
            .Where(ProductFields.ShopId == shopId & OrderFields.OrderStatus == "COMPLETED" & OrderFields.CreatedAt >= last30Days)
            .Select(OrderFields.TotalAmount, OrderFields.CreatedAt);
        
        var dailyOrders = await _adapter.FetchQueryAsync(dailyQuery);
        stats.DailyRevenue = dailyOrders.Cast<object[]>()
            .Select(r => new { Date = (DateTime)r[1], Amount = (decimal)r[0] })
            .GroupBy(x => x.Date.ToString("yyyy-MM-dd"))
            .Select(g => new DailyRevenueDto { Date = g.Key, Revenue = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Date)
            .ToList();

        // 5. Order Status Summary
        var statusSummaryQuery = qf.Create()
            .From(shopOrderJoin)
            .Where(ProductFields.ShopId == shopId)
            .Select(OrderFields.OrderStatus, OrderFields.Id);
        var statusSummaryRows = await _adapter.FetchQueryAsync(statusSummaryQuery);
        stats.OrderStatusSummary = statusSummaryRows.Cast<object[]>()
            .Select(r => new { Status = r[0].ToString()!, OrderId = (Guid)r[1] })
            .GroupBy(x => x.Status)
            .Select(g => new OrderStatusDistributionDto { 
                Status = g.Key, 
                Count = g.Select(x => x.OrderId).Distinct().Count() 
            })
            .ToList();

        // Fill Pending and Processing for frontend
        stats.PendingOrders = stats.OrderStatusSummary.FirstOrDefault(x => x.Status == "PENDING")?.Count ?? 0;
        stats.ProcessingOrders = stats.OrderStatusSummary.FirstOrDefault(x => x.Status == "PROCESSING")?.Count ?? 0;

        // 6. Top Selling Products
        var topProductsQuery = qf.Create()
            .From(qf.Product
                .InnerJoin(qf.ProductVariant).On(ProductFields.Id == ProductVariantFields.ProductId)
                .InnerJoin(qf.OrderItem).On(ProductVariantFields.Id == OrderItemFields.VariantId)
                .InnerJoin(qf.Order).On(OrderItemFields.OrderId == OrderFields.Id))
            .Where(ProductFields.ShopId == shopId & OrderFields.OrderStatus == "COMPLETED")
            .Select(ProductFields.Name, OrderItemFields.Quantity.Sum())
            .GroupBy(ProductFields.Name)
            .OrderBy(OrderItemFields.Quantity.Sum().Descending())
            .Limit(5);
        var topProductsRows = await _adapter.FetchQueryAsync(topProductsQuery);
        stats.TopProducts = topProductsRows.Cast<object[]>()
            .Select(r => new TopProductDto { ProductName = r[0].ToString()!, QuantitySold = Convert.ToInt32(r[1]) })
            .ToList();

        // [DEMO] Add fake data if empty
        if (stats.TotalOrders == 0)
        {
            ApplySellerDemoData(stats);
        }

        return stats;
    }

    private void ApplySellerDemoData(SellerDashboardStatsDto stats)
    {
        if (stats.TotalProducts < 1) stats.TotalProducts = 42;
        stats.TotalOrders = 156;
        stats.TotalRevenue = 85400000; // 85.4M VND
        stats.PendingOrders = 8;
        stats.ProcessingOrders = 15;

        // 30 days daily revenue
        var now = DateTime.UtcNow;
        stats.DailyRevenue = Enumerable.Range(0, 30).Select(i => {
            var date = now.AddDays(-29 + i);
            return new DailyRevenueDto {
                Date = date.ToString("yyyy-MM-dd"),
                Revenue = 1000000 + (new Random().Next(0, 500) * 10000)
            };
        }).ToList();

        stats.OrderStatusSummary = new List<OrderStatusDistributionDto> {
            new() { Status = "PENDING", Count = 8 },
            new() { Status = "PROCESSING", Count = 15 },
            new() { Status = "COMPLETED", Count = 125 },
            new() { Status = "CANCELLED", Count = 8 }
        };

        stats.TopProducts = new List<TopProductDto> {
            new() { ProductName = "iPhone 15 Pro Max 256GB", QuantitySold = 12 },
            new() { ProductName = "AirPods Pro Gen 2", QuantitySold = 45 },
            new() { ProductName = "MacBook Air M2 13 inch", QuantitySold = 8 },
            new() { ProductName = "iPad Pro M2 11 inch", QuantitySold = 15 },
            new() { ProductName = "Apple Watch Series 9", QuantitySold = 22 }
        };
    }
}
