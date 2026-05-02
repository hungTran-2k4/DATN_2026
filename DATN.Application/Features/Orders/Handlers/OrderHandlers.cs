using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using DATN.Application.Features.Orders.Commands;
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

    public CheckoutHandler(
        ICartRepository cartRepo,
        IProductVariantRepository variantRepo,
        IUserAddressRepository addressRepo,
        IOrderRepository orderRepo)
    {
        _cartRepo = cartRepo;
        _variantRepo = variantRepo;
        _addressRepo = addressRepo;
        _orderRepo = orderRepo;
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
            WardId = address.WardId
        });

        // ─── 5. Group items theo ShopId → tạo Order riêng mỗi Shop ───
        var shopGroups = selectedItems
            .GroupBy(i => i.ShopId ?? Guid.Empty)
            .ToList();

        const decimal StandardShippingFee = 30_000m;
        var ordersToCreate = new List<Order>();

        foreach (var shopGroup in shopGroups)
        {
            var items = shopGroup.ToList();
            var productTotal = items.Sum(i => i.UnitPrice * i.Quantity);

            var orderItems = items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                VariantId = i.VariantId,
                ProductNameSnapshot = i.ProductName ?? i.VariantName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                ShippingAddress = addressSnapshot,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "UNPAID",
                OrderStatus = Domain.Entities.Orders.OrderStatus.Pending,
                ShippingFee = StandardShippingFee,
                TotalAmount = productTotal + StandardShippingFee,
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
    public CancelOrderHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

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
        
        // ── If it was already paid, mark as refunded ──
        if (order.PaymentStatus == Domain.Entities.Orders.PaymentStatus.Paid)
        {
            await _orderRepo.UpdatePaymentStatusAsync(request.OrderId, Domain.Entities.Orders.PaymentStatus.Refunded, cancellationToken);
        }

        return ApiResponse<bool>.Succeed(true, "Đơn hàng đã được hủy thành công.");
    }
}

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<bool>>
{
    private readonly IOrderRepository _orderRepo;
    public UpdateOrderStatusHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

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

        // ── If PAID and moving to CANCELLED or RETURNED -> Mark as REFUNDED ──
        if (order.PaymentStatus == Domain.Entities.Orders.PaymentStatus.Paid 
            && (request.NewStatus == Domain.Entities.Orders.OrderStatus.Cancelled || request.NewStatus == Domain.Entities.Orders.OrderStatus.Returned))
        {
            await _orderRepo.UpdatePaymentStatusAsync(request.OrderId, Domain.Entities.Orders.PaymentStatus.Refunded, cancellationToken);
        }

        return ApiResponse<bool>.Succeed(true, $"Đã cập nhật trạng thái đơn hàng thành '{request.NewStatus}'.");
    }
}
