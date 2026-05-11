using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DATN.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly IShippingProvider _shippingProvider;
    private readonly IOrderRepository _orderRepo;
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IShopRepository _shopRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(
        IShippingProvider shippingProvider,
        IOrderRepository orderRepo,
        IShipmentRepository shipmentRepo,
        IShopRepository shopRepo,
        IWalletRepository walletRepo,
        ILogger<ShippingController> logger)
    {
        _shippingProvider = shippingProvider;
        _orderRepo = orderRepo;
        _shipmentRepo = shipmentRepo;
        _shopRepo = shopRepo;
        _walletRepo = walletRepo;
        _logger = logger;
    }

    // ─── 1. TÍNH PHÍ VẬN CHUYỂN (Checkout) ─────────────────
    /// <summary>
    /// Buyer gọi khi ở trang Checkout để tính phí ship động từ GHN.
    /// </summary>
    [Authorize]
    [HttpPost("calculate-fee")]
    public async Task<ActionResult<ApiResponse<ShippingFeeResult>>> CalculateFee(
        [FromBody] ShippingFeeRequest request)
    {
        var result = await _shippingProvider.CalculateFeeAsync(request);

        if (!result.Success)
            return Ok(ApiResponse<ShippingFeeResult>.Fail(
                result.Message ?? "Không thể tính phí vận chuyển.", 400, "SHIPPING_FEE_ERROR"));

        return Ok(ApiResponse<ShippingFeeResult>.Succeed(result, "Tính phí vận chuyển thành công."));
    }

    // ─── 2. TẠO VẬN ĐƠN GHN (Seller gửi hàng) ─────────────
    /// <summary>
    /// Seller nhấn "Gửi hàng" → Backend tạo vận đơn GHN + lưu vào bảng shipments.
    /// </summary>
    [Authorize]
    [HttpPost("create-shipment/{orderId}")]
    public async Task<ActionResult<ApiResponse<CreateShipmentResult>>> CreateShipment(Guid orderId)
    {
        // 1. Lấy thông tin đơn hàng
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order == null)
            return NotFound(ApiResponse<CreateShipmentResult>.Fail("Không tìm thấy đơn hàng.", 404));

        if (order.OrderStatus != OrderStatus.Processing)
            return BadRequest(ApiResponse<CreateShipmentResult>.Fail(
                "Chỉ có thể tạo vận đơn khi đơn hàng ở trạng thái 'Đang xử lý'.", 400));

        // 2. Kiểm tra đã tạo shipment chưa
        var existingShipment = await _shipmentRepo.GetByOrderIdAsync(orderId);
        if (existingShipment != null)
            return BadRequest(ApiResponse<CreateShipmentResult>.Fail(
                "Đơn hàng này đã có vận đơn.", 400));

        // 3. Lấy thông tin Shop (người gửi)
        var shop = order.ShopId.HasValue ? await _shopRepo.GetByIdAsync(order.ShopId.Value) : null;
        if (shop == null)
            return BadRequest(ApiResponse<CreateShipmentResult>.Fail("Không tìm thấy thông tin Shop.", 400));

        // 4. Parse địa chỉ người nhận
        ShippingAddressInfo? buyerAddress = null;
        try
        {
            buyerAddress = JsonSerializer.Deserialize<ShippingAddressInfo>(order.ShippingAddress);
        }
        catch { }

        if (buyerAddress == null)
            return BadRequest(ApiResponse<CreateShipmentResult>.Fail("Không đọc được địa chỉ giao hàng.", 400));

        // 5. Tính COD amount (chỉ thu hộ nếu là COD)
        var codAmount = order.PaymentMethod == PaymentMethod.Cod ? order.TotalAmount : 0;

        // 6. Tạo request gửi GHN
        var createRequest = new CreateShipmentRequest
        {
            ClientOrderCode = order.OrderCode,
            FromName = shop.Name,
            FromPhone = shop.OwnerEmail ?? "", // Dùng tạm, nên có phone trong shop
            FromAddress = shop.PickupAddress ?? "",
            FromDistrictId = shop.DistrictId ?? 0,
            FromWardCode = shop.WardId?.ToString() ?? "",
            ToName = buyerAddress.FullName,
            ToPhone = buyerAddress.PhoneNumber,
            ToAddress = buyerAddress.DetailedAddress,
            ToDistrictId = buyerAddress.DistrictId ?? 0,
            ToWardCode = buyerAddress.WardId?.ToString() ?? "",
            CodAmount = codAmount,
            Weight = 500, // Mặc định, có thể cải thiện sau
            InsuranceValue = (int)order.TotalAmount,
            Note = order.CustomerNote,
            Items = order.Items.Select(i => new ShipmentItem
            {
                Name = i.ProductNameSnapshot ?? "Sản phẩm",
                Quantity = i.Quantity,
                Weight = 500
            }).ToList()
        };

        // 7. Gọi GHN API
        var result = await _shippingProvider.CreateShipmentAsync(createRequest);

        if (!result.Success)
            return BadRequest(ApiResponse<CreateShipmentResult>.Fail(
                result.Message ?? "Không thể tạo vận đơn GHN.", 400));

        // 8. Lưu vào bảng shipments
        await _shipmentRepo.CreateAsync(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Provider = _shippingProvider.ProviderName,
            TrackingCode = result.TrackingCode,
            GhnOrderCode = result.GhnOrderCode,
            ShippingFee = result.TotalFee,
            ExpectedDeliveryDate = result.ExpectedDeliveryDate,
            Status = "PICKED_UP",
            CreatedAt = DateTime.UtcNow
        });

        // 9. Cập nhật trạng thái đơn hàng → SHIPPED
        await _orderRepo.UpdateStatusAsync(orderId, OrderStatus.Shipped);

        return Ok(ApiResponse<CreateShipmentResult>.Succeed(result, "Tạo vận đơn GHN thành công."));
    }

    // ─── 3. WEBHOOK GHN ─────────────────────────────────────
    /// <summary>
    /// Endpoint nhận callback từ GHN khi trạng thái kiện hàng thay đổi.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook/ghn")]
    public async Task<IActionResult> GhnWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("[GHN Webhook] Received: {Body}", body);

            var payload = JsonSerializer.Deserialize<GhnWebhookPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null || string.IsNullOrEmpty(payload.ClientOrderCode))
            {
                _logger.LogWarning("[GHN Webhook] Invalid payload");
                return Ok();
            }

            // Tìm đơn hàng theo OrderCode
            var order = await _orderRepo.GetByOrderCodeAsync(payload.ClientOrderCode);
            if (order == null)
            {
                _logger.LogWarning("[GHN Webhook] Order not found: {Code}", payload.ClientOrderCode);
                return Ok();
            }

            // Guard: đã xử lý rồi thì skip (idempotency đơn giản)
            if (order.OrderStatus == OrderStatus.Delivered ||
                order.OrderStatus == OrderStatus.Cancelled)
            {
                _logger.LogInformation("[GHN Webhook] Order {Code} already in final state: {Status}",
                    payload.ClientOrderCode, order.OrderStatus);
                return Ok();
            }

            // Xử lý trạng thái
            var ghnStatus = payload.Status?.ToLower();

            if (ghnStatus == "delivered")
            {
                await _orderRepo.UpdateStatusAsync(order.Id, OrderStatus.Delivered);

                // ── Trigger escrow settlement (logic đã có sẵn) ──
                if (order.ShopId.HasValue)
                {
                    if (order.PaymentMethod == PaymentMethod.VnPay)
                    {
                        var netAmount = order.TotalAmount - order.CommissionFee;
                        await _walletRepo.UpdateBalanceAsync(
                            order.ShopId.Value, netAmount, "LOCKED",
                            $"Cộng tiền đơn hàng {order.OrderCode} (GHN delivered - Ký quỹ 7 ngày)");
                    }
                    else if (order.PaymentMethod == PaymentMethod.Cod)
                    {
                        await _walletRepo.UpdateBalanceAsync(
                            order.ShopId.Value, -order.CommissionFee, "AVAILABLE",
                            $"Trừ phí sàn đơn hàng {order.OrderCode} (GHN COD delivered)");
                    }
                }

                // Cập nhật shipment status
                var shipment = await _shipmentRepo.GetByOrderIdAsync(order.Id);
                if (shipment != null)
                    await _shipmentRepo.UpdateStatusAsync(shipment.Id, "DELIVERED");

                _logger.LogInformation("[GHN Webhook] ✅ Order {Code} → DELIVERED", payload.ClientOrderCode);
            }
            else if (ghnStatus == "cancel" || ghnStatus == "returned")
            {
                await _orderRepo.UpdateStatusAsync(order.Id, OrderStatus.Returned);

                var shipment = await _shipmentRepo.GetByOrderIdAsync(order.Id);
                if (shipment != null)
                    await _shipmentRepo.UpdateStatusAsync(shipment.Id, "RETURNED");

                _logger.LogInformation("[GHN Webhook] Order {Code} → RETURNED", payload.ClientOrderCode);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GHN Webhook] Error processing");
            return Ok(); // Luôn trả 200 để GHN không retry
        }
    }

    // ─── 4. TRACKING INFO ───────────────────────────────────
    /// <summary>
    /// Lấy thông tin vận chuyển của đơn hàng.
    /// </summary>
    [Authorize]
    [HttpGet("tracking/{orderId}")]
    public async Task<ActionResult<ApiResponse<Shipment>>> GetTracking(Guid orderId)
    {
        var shipment = await _shipmentRepo.GetByOrderIdAsync(orderId);
        if (shipment == null)
            return NotFound(ApiResponse<Shipment>.Fail("Chưa có thông tin vận chuyển cho đơn hàng này.", 404));

        return Ok(ApiResponse<Shipment>.Succeed(shipment));
    }

    // ─── 5. GHN ADDRESS PROXY (cho Frontend dùng mã GHN) ────

    /// <summary>Lấy danh sách tỉnh/thành phố theo mã GHN</summary>
    [HttpGet("ghn/provinces")]
    [ProducesResponseType(typeof(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnProvince>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGhnProvinces()
    {
        if (_shippingProvider is DATN.Infrastructure.Services.Shipping.GHNProvider ghn)
        {
            var provinces = await ghn.GetProvincesAsync();
            return Ok(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnProvince>>.Succeed(provinces));
        }
        return BadRequest(ApiResponse<object>.Fail("Shipping provider không hỗ trợ.", 400));
    }

    /// <summary>Lấy danh sách quận/huyện theo ProvinceID GHN</summary>
    [HttpGet("ghn/districts/{provinceId}")]
    [ProducesResponseType(typeof(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnDistrict>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGhnDistricts(int provinceId)
    {
        if (_shippingProvider is DATN.Infrastructure.Services.Shipping.GHNProvider ghn)
        {
            var districts = await ghn.GetDistrictsAsync(provinceId);
            return Ok(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnDistrict>>.Succeed(districts));
        }
        return BadRequest(ApiResponse<object>.Fail("Shipping provider không hỗ trợ.", 400));
    }

    /// <summary>Lấy danh sách phường/xã theo DistrictID GHN</summary>
    [HttpGet("ghn/wards/{districtId}")]
    [ProducesResponseType(typeof(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnWard>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGhnWards(int districtId)
    {
        if (_shippingProvider is DATN.Infrastructure.Services.Shipping.GHNProvider ghn)
        {
            var wards = await ghn.GetWardsAsync(districtId);
            return Ok(ApiResponse<List<DATN.Infrastructure.Services.Shipping.GhnWard>>.Succeed(wards));
        }
        return BadRequest(ApiResponse<object>.Fail("Shipping provider không hỗ trợ.", 400));
    }
}

// ─── Helper Models ──────────────────────────────────────

internal class ShippingAddressInfo
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = "";

    [JsonPropertyName("detailedAddress")]
    public string DetailedAddress { get; set; } = "";

    [JsonPropertyName("provinceId")]
    public int? ProvinceId { get; set; }

    [JsonPropertyName("districtId")]
    public int? DistrictId { get; set; }

    [JsonPropertyName("wardId")]
    public int? WardId { get; set; }
}

internal class GhnWebhookPayload
{
    [JsonPropertyName("ClientOrderCode")]
    public string? ClientOrderCode { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("OrderCode")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }
}
