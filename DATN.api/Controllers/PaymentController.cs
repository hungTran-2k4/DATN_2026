using System.Text.Json;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentProviderFactory providerFactory,
        IOrderRepository orderRepo,
        IPaymentRepository paymentRepo,
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        ILogger<PaymentController> logger)
    {
        _providerFactory = providerFactory;
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _logger = logger;
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  POST /api/payments/create-payment-url                         ║
    // ║  Tạo URL thanh toán — hỗ trợ đa cổng (VNPay, MoMo, ...)      ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Tạo URL thanh toán cho đơn hàng.
    /// FE gọi sau khi checkout thành công, truyền OrderId.
    /// Tự động resolve đúng gateway dựa trên PaymentMethod của Order.
    /// </summary>
    [HttpPost("create-payment-url")]
    [Authorize]
    [ProducesResponseType(typeof(CreatePaymentUrlResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlRequest request)
    {
        // Support both single OrderId (legacy) and multiple OrderIds
        var orderIds = request.OrderIds?.Count > 0 
            ? request.OrderIds 
            : new List<Guid> { request.OrderId };

        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1. Load tất cả đơn hàng
        var orders = new List<Domain.Entities.Orders.Order>();
        foreach (var orderId in orderIds)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng {orderId}." });
            if (order.BuyerId != currentUserId)
                return StatusCode(403, new { success = false, message = "Không có quyền thao tác đơn hàng này." });
            if (order.PaymentStatus == PaymentStatus.Paid)
                continue; // skip already paid
            orders.Add(order);
        }

        if (orders.Count == 0)
            return BadRequest(new { success = false, message = "Tất cả đơn hàng đã được thanh toán." });

        // 2. Tính tổng tiền
        var totalAmount = orders.Sum(o => o.TotalAmount);

        // 3. Tạo TxnRef gộp: dùng order đầu tiên làm ID chính, lưu danh sách trong bảng Payment
        var primaryOrderId = orders[0].Id;
        var provider = _providerFactory.GetProvider(orders[0].PaymentMethod!);
        if (provider == null)
            return BadRequest(new { success = false, message = $"Phương thức thanh toán không hỗ trợ online payment." });

        // 4. Lấy IP client
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        var orderCodes = string.Join(", ", orders.Select(o => o.OrderCode));
        var orderInfo = $"Thanh toan don hang {orderCodes}";

        // 5. Tạo URL — dùng primary order ID làm TxnRef
        var paymentUrl = provider.CreatePaymentUrl(primaryOrderId, totalAmount, orderInfo, ipAddress);

        // 6. Tạo Payment record cho mỗi order
        foreach (var order in orders)
        {
            await _paymentRepo.CreateAsync(new Domain.Entities.Orders.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = provider.ProviderName,
                Amount = order.TotalAmount,
                Status = PaymentRecordStatus.Pending,
                Currency = "VND",
                // Lưu primary order ID để nhóm các đơn cùng 1 giao dịch
                RawResponse = $"grouped_with:{primaryOrderId}",
                CreatedAt = DateTime.UtcNow
            });

            await _transactionRepo.CreateAsync(new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Provider = provider.ProviderName,
                Status = "Pending",
                TransactionType = "PAYMENT_IN",
                CreatedAt = DateTime.UtcNow
            });

            await _orderRepo.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Processing);
        }

        _logger.LogInformation(
            "[Payment] Created GROUPED payment URL. PrimaryOrderId={PrimaryOrderId}, OrderCount={Count}, TotalAmount={Amount}",
            primaryOrderId, orders.Count, totalAmount);

        return Ok(new CreatePaymentUrlResponse
        {
            Success = true,
            PaymentUrl = paymentUrl
        });
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  GET /api/payments/vnpay-ipn                                   ║
    // ║  IPN = Source of Truth — DUY NHẤT được phép update DB          ║
    // ║  ❌ Return API KHÔNG update DB                                  ║
    // ║  ✅ Chỉ IPN được phép update trạng thái                        ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// IPN URL — VNPay gọi ngầm (server-to-server).
    /// Đây là SOURCE OF TRUTH — chỗ DUY NHẤT cập nhật trạng thái thanh toán.
    /// Có idempotency check, validate amount, transaction, logging đầy đủ.
    /// VNPay retry tối đa 10 lần, mỗi lần cách 5 phút.
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn()
    {
        // ─── Log toàn bộ IPN request ───
        var rawQuery = Request.QueryString.ToString();
        _logger.LogInformation("[IPN] VNPay IPN received. RawQuery={RawQuery}", rawQuery);

        // ─── 1. Resolve provider ───
        var provider = _providerFactory.GetProvider(PaymentMethod.VnPay);
        if (provider == null)
        {
            _logger.LogError("[IPN] VNPay provider not found in factory");
            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }

        // ─── 2. Parse và validate signature ───
        var queryDict = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());
        var result = provider.HandleIpn(queryDict);

        _logger.LogInformation(
            "[IPN] Signature validation: IsValid={IsValid}, OrderId={OrderId}, ResponseCode={ResponseCode}, TransactionId={TransactionId}",
            result.ResponseCode != "97", result.OrderId, result.ResponseCode, result.TransactionId);

        // Signature invalid
        if (result.ResponseCode == "97")
        {
            _logger.LogWarning("[IPN] Invalid signature! RawQuery={RawQuery}", rawQuery);
            return Ok(new { RspCode = "97", Message = "Invalid signature" });
        }

        // OrderId parse failed
        if (result.ResponseCode == "01")
        {
            _logger.LogWarning("[IPN] Invalid order reference. TxnRef={TxnRef}", Request.Query["vnp_TxnRef"].ToString());
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        // ─── 3. Tìm đơn hàng ───
        var order = await _orderRepo.GetByIdAsync(result.OrderId);
        if (order == null)
        {
            _logger.LogWarning("[IPN] Order not found. OrderId={OrderId}", result.OrderId);
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        // ─── 4. Validate amount — CRITICAL SECURITY CHECK ───
        if ((long)order.TotalAmount != (long)result.Amount)
        {
            _logger.LogError(
                "[IPN] AMOUNT MISMATCH! OrderId={OrderId}, Expected={Expected}, Received={Received}",
                result.OrderId, order.TotalAmount, result.Amount);
            return Ok(new { RspCode = "04", Message = "Invalid amount" });
        }

        // ─── 5. Idempotency — đã xử lý trước đó thì skip ───
        if (order.PaymentStatus == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.Failed)
        {
            _logger.LogInformation(
                "[IPN] Order already confirmed. OrderId={OrderId}, CurrentStatus={Status}",
                result.OrderId, order.PaymentStatus);
            return Ok(new { RspCode = "02", Message = "Order already confirmed" });
        }

        // Idempotency bổ sung: check TransactionId trong bảng payments
        if (!string.IsNullOrEmpty(result.TransactionId))
        {
            var existingPayment = await _paymentRepo.GetByTransactionIdAsync(result.TransactionId, PaymentMethod.VnPay);
            if (existingPayment != null && existingPayment.Status == PaymentRecordStatus.Success)
            {
                _logger.LogInformation(
                    "[IPN] Transaction already processed. TransactionId={TransactionId}",
                    result.TransactionId);
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            }
        }

        // ─── 6. Cập nhật DB — trong transaction ───
        try
        {
            // Tìm tất cả order cùng nhóm thanh toán (grouped_with:primaryOrderId)
            var relatedOrderIds = await GetGroupedOrderIds(result.OrderId);

            foreach (var relatedOrderId in relatedOrderIds)
            {
                var relatedOrder = await _orderRepo.GetByIdAsync(relatedOrderId);
                if (relatedOrder == null || relatedOrder.PaymentStatus == PaymentStatus.Paid) continue;

                // Cập nhật Payment record
                var paymentRecord = await _paymentRepo.GetByOrderIdAsync(relatedOrderId);
                if (paymentRecord != null)
                {
                    paymentRecord.TransactionId = result.TransactionId;
                    paymentRecord.Status = result.IsSuccess ? PaymentRecordStatus.Success : PaymentRecordStatus.Failed;
                    paymentRecord.ResponseCode = result.ResponseCode;
                    paymentRecord.BankCode = result.BankCode;
                    paymentRecord.CardType = result.CardType;
                    paymentRecord.PayDate = result.PayDate;
                    paymentRecord.RawResponse = result.RawResponse;
                    paymentRecord.Signature = result.Signature;
                    await _paymentRepo.UpdateAsync(paymentRecord);
                }

                // Cập nhật Order.PaymentStatus
                var newPaymentStatus = result.IsSuccess ? PaymentStatus.Paid : PaymentStatus.Failed;
                await _orderRepo.UpdatePaymentStatusAsync(relatedOrderId, newPaymentStatus);

                _logger.LogInformation(
                    "[IPN] ✅ Order updated. OrderId={OrderId}, PaymentStatus={Status}, TransactionId={TransactionId}",
                    relatedOrderId, newPaymentStatus, result.TransactionId);

                if (result.IsSuccess)
                {
                    await _transactionRepo.CreateAsync(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        OrderId = relatedOrderId,
                        ExternalTransactionNo = result.TransactionId,
                        Amount = relatedOrder.TotalAmount,
                        Provider = PaymentMethod.VnPay,
                        Status = "Success",
                        TransactionType = "PAYMENT_IN",
                        RawResponse = result.RawResponse,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[IPN] ❌ Database update failed! OrderId={OrderId}, TransactionId={TransactionId}",
                result.OrderId, result.TransactionId);
            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  GET /api/payments/vnpay-return                                ║
    // ║  ❌ KHÔNG UPDATE DB — chỉ validate + trả kết quả cho FE       ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Return URL — VNPay chuyển hướng trình duyệt KH về đây sau thanh toán.
    /// ❌ KHÔNG cập nhật DB — chỉ validate signature và trả kết quả cho FE hiển thị.
    /// FE forward toàn bộ query params xuống API này, rồi hiển thị theo response.
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        var queryDict = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());

        var provider = _providerFactory.GetProvider(PaymentMethod.VnPay);
        if (provider == null)
            return Ok(new PaymentReturnResponse { IsSuccess = false, Message = "Provider not found" });

        var result = provider.HandleReturn(queryDict);
        _logger.LogInformation("═══ VnPayReturn ═══ OrderId={OrderId}, IsSuccess={IsSuccess}, ResponseCode={ResponseCode}",
            result.OrderId, result.IsSuccess, result.ResponseCode);

        var order = await _orderRepo.GetByIdAsync(result.OrderId);
        var orderCodes = new List<string>();
        if (order != null) orderCodes.Add(order.OrderCode!);

        // ─── LOCAL FALLBACK ───
        if (result.IsSuccess && result.ResponseCode == "00")
        {
            _logger.LogInformation("═══ VnPayReturn ═══ Entering FALLBACK/SYNC update for OrderId={OrderId}", result.OrderId);
            try
            {
                var relatedOrderIds = await GetGroupedOrderIds(result.OrderId);
                orderCodes.Clear(); // Clear and refill with all related codes

                foreach (var relatedOrderId in relatedOrderIds)
                {
                    var relatedOrder = await _orderRepo.GetByIdAsync(relatedOrderId);
                    if (relatedOrder == null) continue;
                    
                    orderCodes.Add(relatedOrder.OrderCode!);

                    if (relatedOrder.PaymentStatus != PaymentStatus.Paid)
                    {
                        var paymentRecord = await _paymentRepo.GetByOrderIdAsync(relatedOrderId);
                        if (paymentRecord != null)
                        {
                            paymentRecord.TransactionId = result.TransactionId;
                            paymentRecord.Status = PaymentRecordStatus.Success;
                            paymentRecord.ResponseCode = result.ResponseCode;
                            paymentRecord.PayDate = result.PayDate;
                            await _paymentRepo.UpdateAsync(paymentRecord);
                        }

                        await _orderRepo.UpdatePaymentStatusAsync(relatedOrderId, PaymentStatus.Paid);

                        await _transactionRepo.CreateAsync(new Transaction
                        {
                            Id = Guid.NewGuid(),
                            OrderId = relatedOrderId,
                            ExternalTransactionNo = result.TransactionId,
                            Amount = relatedOrder.TotalAmount,
                            Provider = PaymentMethod.VnPay,
                            Status = "Success",
                            TransactionType = "PAYMENT_IN",
                            CreatedAt = DateTime.UtcNow
                        });
                        
                        _logger.LogInformation("═══ VnPayReturn ═══ Fallback updated OrderId={OrderId}", relatedOrderId);
                    }
                }

                // Refresh primary order data
                order = await _orderRepo.GetByIdAsync(result.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order status in ReturnURL fallback for OrderId={OrderId}", result.OrderId);
            }
        }

        return Ok(new PaymentReturnResponse
        {
            IsSuccess = result.IsSuccess && result.ResponseCode == "00",
            OrderId = result.OrderId.ToString(),
            OrderCode = order?.OrderCode,
            OrderCodes = orderCodes, // Trả về danh sách mã đơn
            Amount = result.Amount,
            TransactionId = result.TransactionId,
            BankCode = result.BankCode,
            PayDate = result.PayDate,
            PaymentStatus = order?.PaymentStatus,
            Message = result.IsSuccess ? "Giao dịch thành công" : "Giao dịch không thành công"
        });
    }

    /// <summary>
    /// Tìm tất cả OrderIds thuộc cùng 1 nhóm thanh toán gộp.
    /// Nhóm được xác định bằng RawResponse chứa "grouped_with:{primaryOrderId}".
    /// Nếu không tìm thấy nhóm, trả về chính orderId đó.
    /// </summary>
    private async Task<List<Guid>> GetGroupedOrderIds(Guid primaryOrderId)
    {
        var groupKey = $"grouped_with:{primaryOrderId}";
        var groupedPayments = await _paymentRepo.GetByGroupKeyAsync(groupKey);
        
        if (groupedPayments != null && groupedPayments.Any())
        {
            return groupedPayments.Select(p => p.OrderId).Distinct().ToList();
        }
        
        // Fallback: chỉ trả về chính orderId (single-order payment hoặc legacy)
        return new List<Guid> { primaryOrderId };
    }
}

// ──── Request / Response Models ────

public class CreatePaymentUrlRequest
{
    public Guid OrderId { get; set; }
    /// <summary>Danh sách OrderIds cho thanh toán gộp (multi-shop checkout)</summary>
    public List<Guid>? OrderIds { get; set; }
}

public class CreatePaymentUrlResponse
{
    public bool Success { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response chuẩn hóa cho Return URL — FE chỉ đọc và hiển thị.
/// </summary>
public class PaymentReturnResponse
{
    public bool IsSuccess { get; set; }
    public string? OrderId { get; set; }
    public string? OrderCode { get; set; }
    public List<string>? OrderCodes { get; set; } // Danh sách mã các đơn hàng trong nhóm
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? BankCode { get; set; }
    public string? PayDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string Message { get; set; } = string.Empty;
}
