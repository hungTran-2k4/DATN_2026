namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Bản ghi giao dịch thanh toán — lưu toàn bộ response từ gateway để đối soát.
/// Mỗi lần gọi IPN tạo/cập nhật 1 bản ghi.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    /// <summary>Liên kết tới Order</summary>
    public Guid OrderId { get; set; }

    /// <summary>Cổng thanh toán: VNPAY, MOMO, ZALOPAY</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Mã giao dịch do Gateway trả về (vnp_TransactionNo)</summary>
    public string? TransactionId { get; set; }

    /// <summary>Số tiền giao dịch (VNĐ, đã chia 100 từ VNPay)</summary>
    public decimal Amount { get; set; }

    /// <summary>Trạng thái: PENDING, SUCCESS, FAILED</summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>Mã phản hồi gốc từ Gateway (00, 24, ...)</summary>
    public string? ResponseCode { get; set; }

    /// <summary>Ngân hàng thanh toán (NCB, VCB, ...)</summary>
    public string? BankCode { get; set; }

    /// <summary>Loại thẻ (ATM, QRCODE, ...)</summary>
    public string? CardType { get; set; }

    /// <summary>Ngày thanh toán — format gốc từ Gateway (yyyyMMddHHmmss)</summary>
    public string? PayDate { get; set; }

    /// <summary>Toàn bộ raw response JSON từ Gateway — để debug/audit</summary>
    public string? RawResponse { get; set; }

    /// <summary>Chữ ký nhận được — để debug bảo mật</summary>
    public string? Signature { get; set; }

    /// <summary>Đơn vị tiền tệ</summary>
    public string Currency { get; set; } = "VND";

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Trạng thái của bản ghi Payment</summary>
public static class PaymentRecordStatus
{
    public const string Pending = "PENDING";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
}
