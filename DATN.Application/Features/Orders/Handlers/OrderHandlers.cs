using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using DATN.Application.Features.Orders.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace DATN.Application.Features.Orders.Handlers;

/// <summary>
/// Core checkout logic:
/// 1. Lấy cart items được chọn
/// 2. Validate từng item (còn hàng, giá hợp lệ)
/// 3. Load địa chỉ giao hàng
/// 4. Group items theo ShopId → tạo Order riêng cho từng Shop
/// 5. Deduct stock cho từng variant
/// 6. Xóa cart items đã checkout
/// </summary>
public class CheckoutHandler : IRequestHandler<CheckoutCommand, ApiResponse<IEnumerable<OrderSummaryDto>>>
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductVariantRepository _variantRepo;
    private readonly IUserAddressRepository _addressRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IShippingProvider _shippingProvider;
    private readonly IShopRepository _shopRepo;

    public CheckoutHandler(
        ICartRepository cartRepo,
        IProductVariantRepository variantRepo,
        IUserAddressRepository addressRepo,
        IOrderRepository orderRepo,
        IShippingProvider shippingProvider,
        IShopRepository shopRepo)
    {
        _cartRepo = cartRepo;
        _variantRepo = variantRepo;
        _addressRepo = addressRepo;
        _orderRepo = orderRepo;
        _shippingProvider = shippingProvider;
        _shopRepo = shopRepo;
    }

    public async Task<ApiResponse<IEnumerable<OrderSummaryDto>>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // ─── 1. Lấy cart items được chọn ───
        var allItems = await _cartRepo.GetByUserIdAsync(request.BuyerId, cancellationToken);
        var selectedItems = allItems
            .Where(i => request.CartItemIds.Contains(i.Id))
            .ToList();

        if (!selectedItems.Any())
            return ApiResponse<IEnumerable<OrderSummaryDto>>.Fail("Không có sản phẩm nào được chọn.", 400, "NO_ITEMS_SELECTED");

        // ─── 2. Validate tồn kho từng item ───
        var stockErrors = new List<string>();
        foreach (var item in selectedItems)
        {
            if (item.VariantId == null)
            {
                stockErrors.Add($"Cart item {item.Id} không hợp lệ.");
                continue;
            }
            var stock = await _variantRepo.GetStockQtyAsync(item.VariantId.Value, cancellationToken);
            if (stock < item.Quantity)
                stockErrors.Add($"Sản phẩm '{item.ProductName}' chỉ còn {stock} trong kho, bạn đang đặt {item.Quantity}.");
        }

        if (stockErrors.Any())
            return ApiResponse<IEnumerable<OrderSummaryDto>>.Fail(
                "Một số sản phẩm không đủ tồn kho: " + string.Join("; ", stockErrors),
                400, "INSUFFICIENT_STOCK");

        // ─── 3. Validate phương thức thanh toán ───
        if (request.PaymentMethod != Domain.Entities.Orders.PaymentMethod.Cod
            && request.PaymentMethod != Domain.Entities.Orders.PaymentMethod.BankTransfer
            && request.PaymentMethod != Domain.Entities.Orders.PaymentMethod.VnPay)
            return ApiResponse<IEnumerable<OrderSummaryDto>>.Fail("Phương thức thanh toán không hợp lệ.", 400, "INVALID_PAYMENT_METHOD");

        // ─── 4. Lấy địa chỉ giao hàng và tạo snapshot ───
        var address = await _addressRepo.GetByIdAsync(request.ShippingAddressId, request.BuyerId, cancellationToken);
        if (address == null)
            return ApiResponse<IEnumerable<OrderSummaryDto>>.Fail("Địa chỉ giao hàng không tồn tại.", 404, "ADDRESS_NOT_FOUND");

        var addressSnapshot = JsonSerializer.Serialize(new ShippingAddressSnapshot
        {
            FullName = address.FullName,
            PhoneNumber = address.PhoneNumber,
            DetailedAddress = address.DetailedAddress,
            ProvinceId = address.ProvinceId,
            DistrictId = address.DistrictId,
            WardId = address.WardId.ToString()
        });

        // ─── 5. Group items theo ShopId → tạo Order riêng mỗi Shop ───
        var shopGroups = selectedItems
            .GroupBy(i => i.ShopId ?? Guid.Empty)
            .ToList();

        var ordersToCreate = new List<Order>();

        foreach (var shopGroup in shopGroups)
        {
            var items = shopGroup.ToList();
            var productTotal = items.Sum(i => i.UnitPrice * i.Quantity);

            // ── Tính phí ship động từ GHN API ──
            decimal shippingFee = 30_000m; // Fallback
            try
            {
                var shop = await _shopRepo.GetByIdAsync(shopGroup.Key, cancellationToken);
                if (shop != null && shop.DistrictId.HasValue)
                {
                    var totalWeight = items.Sum(i => i.Quantity) * 500; // Mặc định 500g/item
                    var feeResult = await _shippingProvider.CalculateFeeAsync(new ShippingFeeRequest
                    {
                        FromDistrictId = shop.DistrictId.Value,
                        FromWardCode = shop.WardId?.ToString() ?? "",
                        ToDistrictId = address.DistrictId ?? 0,
                        ToWardCode = address.WardId?.ToString() ?? "",
                        Weight = totalWeight > 0 ? totalWeight : 500,
                        InsuranceValue = Math.Min((int)productTotal, 5000000)
                    });

                    if (feeResult.Success)
                        shippingFee = feeResult.TotalFee;
                }
            }
            catch { /* Fallback to 30k if GHN API fails */ }

            var orderItems = items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                VariantId = i.VariantId,
                ProductNameSnapshot = i.ProductName ?? i.VariantName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            var commissionRate = 0.05m; // 5% phí sàn
            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                ShopId = shopGroup.Key,
                ShippingAddress = addressSnapshot,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "UNPAID",
                OrderStatus = Domain.Entities.Orders.OrderStatus.Pending,
                ShippingFee = shippingFee,
                TotalAmount = productTotal + shippingFee,
                CommissionFee = productTotal * commissionRate,
                CustomerNote = request.CustomerNote,
                CreatedAt = DateTime.UtcNow,
                Items = orderItems
            };

            ordersToCreate.Add(order);
        }

        // ─── 6. Lưu tất cả orders vào DB ───
        var createdOrders = (await _orderRepo.CreateBulkAsync(ordersToCreate, cancellationToken)).ToList();

        // ─── 7. Deduct stock cho từng variant đã được đặt ───
        foreach (var item in selectedItems)
        {
            await _variantRepo.DeductStockAsync(item.VariantId!.Value, item.Quantity, cancellationToken);
        }

        // ─── 8. Xóa cart items đã checkout ───
        var variantIds = selectedItems.Select(i => i.VariantId!.Value).ToList();
        await _cartRepo.RemoveByVariantIdsAsync(request.BuyerId, variantIds, cancellationToken);

        // ─── 9. Trả về danh sách orders tóm tắt ───
        var summaries = createdOrders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            OrderStatus = o.OrderStatus,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            TotalAmount = o.TotalAmount,
            TotalItems = o.Items.Count,
            FirstItemName = o.Items.FirstOrDefault()?.ProductNameSnapshot,
            CreatedAt = o.CreatedAt
        });

        return ApiResponse<IEnumerable<OrderSummaryDto>>.Succeed(summaries, $"Đặt hàng thành công. Tạo {createdOrders.Count} đơn hàng.", 201);
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, ApiResponse<bool>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IWalletRepository _walletRepo;
    public CancelOrderHandler(IOrderRepository orderRepo, IWalletRepository walletRepo)
    {
        _orderRepo = orderRepo;
        _walletRepo = walletRepo;
    }

    public async Task<ApiResponse<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.", 404, "ORDER_NOT_FOUND");

        if (order.BuyerId != request.BuyerId)
            return ApiResponse<bool>.Fail("Không có quyền thao tác đơn hàng này.", 403, "ORDER_FORBIDDEN");

        if (order.OrderStatus != Domain.Entities.Orders.OrderStatus.Pending)
            return ApiResponse<bool>.Fail(
                "Chỉ có thể hủy đơn hàng đang ở trạng thái 'Chờ xác nhận'.",
                400, "CANNOT_CANCEL");

        await _orderRepo.UpdateStatusAsync(request.OrderId, Domain.Entities.Orders.OrderStatus.Cancelled, cancellationToken);

        return ApiResponse<bool>.Succeed(true, "Đơn hàng đã được hủy thành công.");
    }
}

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<bool>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IShippingProvider _shippingProvider;

    public UpdateOrderStatusHandler(
        IOrderRepository orderRepo, 
        IWalletRepository walletRepo,
        IShipmentRepository shipmentRepo,
        IShippingProvider shippingProvider)
    {
        _orderRepo = orderRepo;
        _walletRepo = walletRepo;
        _shipmentRepo = shipmentRepo;
        _shippingProvider = shippingProvider;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.", 404, "ORDER_NOT_FOUND");

        if (!Domain.Entities.Orders.OrderStatus.IsValidTransition(order.OrderStatus!, request.NewStatus))
            return ApiResponse<bool>.Fail(
                $"Không thể chuyển từ '{order.OrderStatus}' sang '{request.NewStatus}'.",
                400, "INVALID_STATUS_TRANSITION");

        // ── Validation: VNPAY order must be PAID before processing ──
        if (order.PaymentMethod == Domain.Entities.Orders.PaymentMethod.VnPay 
            && order.OrderStatus == Domain.Entities.Orders.OrderStatus.Pending 
            && request.NewStatus == Domain.Entities.Orders.OrderStatus.Processing
            && order.PaymentStatus != Domain.Entities.Orders.PaymentStatus.Paid)
        {
            return ApiResponse<bool>.Fail(
                "Đơn hàng VNPay chưa được thanh toán. Không thể xác nhận đơn.",
                400, "VNPAY_NOT_PAID");
        }

        await _orderRepo.UpdateStatusAsync(request.OrderId, request.NewStatus, cancellationToken);

        // ── NEW: If moving to CANCELLED and has Shipment -> Cancel at GHN ──
        if (request.NewStatus == Domain.Entities.Orders.OrderStatus.Cancelled)
        {
            var shipment = await _shipmentRepo.GetByOrderIdAsync(request.OrderId, cancellationToken);
            if (shipment != null && !string.IsNullOrEmpty(shipment.GhnOrderCode))
            {
                await _shippingProvider.CancelShipmentAsync(shipment.GhnOrderCode);
                await _shipmentRepo.UpdateStatusAsync(shipment.Id, "CANCELLED", cancellationToken);
            }
        }



        // ── NEW: If DELIVERED -> Update Shop Wallet ──
        if (request.NewStatus == Domain.Entities.Orders.OrderStatus.Delivered && order.ShopId.HasValue)
        {
            if (order.PaymentMethod == Domain.Entities.Orders.PaymentMethod.VnPay)
            {
                // VNPay: Sàn giữ tiền -> Trả cho Seller = Tổng - Phí Ship - Phí Sàn -> LOCKED
                var netAmount = order.TotalAmount - (order.ShippingFee ?? 0m) - order.CommissionFee;
                var description = $"Cộng tiền đơn hàng {order.OrderCode} (Thanh toán online, đã trừ phí sàn {order.CommissionFee:N0} và phí ship {order.ShippingFee:N0} - Chờ xác nhận 7 ngày)";
                
                await _walletRepo.UpdateBalanceAsync(
                    order.ShopId.Value, 
                    netAmount, 
                    "LOCKED", 
                    description, 
                    null, 
                    cancellationToken);
            }
            else if (order.PaymentMethod == Domain.Entities.Orders.PaymentMethod.Cod)
            {
                // COD: Shipper thu hộ -> Sàn giữ -> Cộng vào LOCKED cho Seller
                // Net Amount = Tổng - Phí Ship - Phí Sàn
                var netAmount = order.TotalAmount - (order.ShippingFee ?? 0m) - order.CommissionFee;
                var description = $"Cộng tiền đơn hàng {order.OrderCode} (Thanh toán COD - Chờ người mua xác nhận hoặc quá hạn 7 ngày)";
                
                await _walletRepo.UpdateBalanceAsync(
                    order.ShopId.Value, 
                    netAmount, 
                    "LOCKED", 
                    description, 
                    null, 
                    cancellationToken);
            }
        }

        return ApiResponse<bool>.Succeed(true, $"Đã cập nhật trạng thái đơn hàng thành '{request.NewStatus}'.");
    }
}

public class ConfirmOrderReceivedHandler : IRequestHandler<ConfirmOrderReceivedCommand, ApiResponse<bool>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IWalletRepository _walletRepo;

    public ConfirmOrderReceivedHandler(IOrderRepository orderRepo, IWalletRepository walletRepo)
    {
        _orderRepo = orderRepo;
        _walletRepo = walletRepo;
    }

    public async Task<ApiResponse<bool>> Handle(ConfirmOrderReceivedCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.", 404, "ORDER_NOT_FOUND");

        if (order.BuyerId != request.BuyerId)
            return ApiResponse<bool>.Fail("Không có quyền thao tác đơn hàng này.", 403, "ORDER_FORBIDDEN");

        if (order.OrderStatus != Domain.Entities.Orders.OrderStatus.Delivered)
            return ApiResponse<bool>.Fail(
                "Chỉ có thể xác nhận khi đơn hàng ở trạng thái 'Đã giao hàng'.",
                400, "INVALID_STATUS_FOR_CONFIRMATION");

        // 1. Cập nhật trạng thái đơn hàng sang COMPLETED
        var ok = await _orderRepo.UpdateStatusAsync(request.OrderId, Domain.Entities.Orders.OrderStatus.Completed, cancellationToken);
        if (!ok) return ApiResponse<bool>.Fail("Lỗi cập nhật trạng thái đơn hàng.", 500);

        // 2. Giải phóng tiền ký quỹ cho Shop (nếu có)
        // Chỉ giải phóng nếu là VNPay (vì COD Shop đã nhận tiền mặt từ shipper/khách)
        if (order.PaymentMethod == Domain.Entities.Orders.PaymentMethod.VnPay && order.ShopId.HasValue)
        {
            var netAmount = order.TotalAmount - (order.ShippingFee ?? 0m) - order.CommissionFee;
            var description = $"Giải phóng tiền đơn hàng {order.OrderCode} (Người mua đã xác nhận sớm)";
            await _walletRepo.ReleaseLockedFundsAsync(order.ShopId.Value, netAmount, description, cancellationToken);
        }

        return ApiResponse<bool>.Succeed(true, "Xác nhận đã nhận hàng thành công. Tiền đã được chuyển vào ví của người bán.");
    }
}
