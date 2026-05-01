using DATN.Domain.Entities.Orders;

namespace DATN.Application.Interfaces.Services;

/// <summary>
/// Kết quả thanh toán chuẩn hóa — dùng chung cho mọi gateway (VNPay, MoMo, ZaloPay).
/// Mỗi Provider tự map response về format này.
/// </summary>
public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? ResponseCode { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public string? PayDate { get; set; }
    public string? Message { get; set; }

    /// <summary>Raw response JSON từ gateway — lưu nguyên vào DB để audit</summary>
    public string? RawResponse { get; set; }

    /// <summary>Chữ ký nhận được từ gateway</summary>
    public string? Signature { get; set; }
}

/// <summary>
/// Abstraction cho cổng thanh toán — Strategy Pattern.
/// Mỗi gateway (VNPay, MoMo, ...) implement interface này.
/// Khi thêm cổng mới chỉ cần tạo class mới, không sửa core logic.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Tên provider (VNPAY, MOMO, ZALOPAY)</summary>
    string ProviderName { get; }

    /// <summary>
    /// Tạo URL chuyển hướng tới cổng thanh toán.
    /// </summary>
    string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo, string ipAddress);

    /// <summary>
    /// Xử lý IPN callback từ gateway.
    /// Validate signature + parse response → PaymentResult chuẩn hóa.
    /// </summary>
    PaymentResult HandleIpn(IDictionary<string, string> data);

    /// <summary>
    /// Validate chữ ký từ Return URL (chỉ để hiển thị, không cập nhật DB).
    /// </summary>
    PaymentResult HandleReturn(IDictionary<string, string> data);
}

/// <summary>
/// Factory để resolve đúng IPaymentProvider theo paymentMethod.
/// Tránh if/else spaghetti trong Controller.
/// </summary>
public interface IPaymentProviderFactory
{
    /// <summary>
    /// Lấy provider phù hợp theo tên phương thức (VNPAY, MOMO, ...).
    /// Trả về null nếu không hỗ trợ.
    /// </summary>
    IPaymentProvider? GetProvider(string paymentMethod);

    /// <summary>Kiểm tra phương thức thanh toán có phải online payment không</summary>
    bool IsOnlinePayment(string paymentMethod);
}
