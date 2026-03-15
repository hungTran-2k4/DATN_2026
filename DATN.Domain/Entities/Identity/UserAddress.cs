namespace DATN.Domain.Entities.Identity;

/// <summary>
/// Sổ địa chỉ giao/nhận hàng của người dùng
/// </summary>
public class UserAddress
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }

    /// <summary>Tên người nhận hàng</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Số điện thoại người nhận</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Mã tỉnh/thành phố (từ dữ liệu hành chính VN)</summary>
    public int? ProvinceId { get; set; }

    /// <summary>Mã quận/huyện</summary>
    public int? DistrictId { get; set; }

    /// <summary>Mã phường/xã</summary>
    public int? WardId { get; set; }

    /// <summary>Địa chỉ chi tiết (số nhà, tên đường, ...)</summary>
    public string DetailedAddress { get; set; } = string.Empty;

    /// <summary>Địa chỉ mặc định để hiện lên khi checkout</summary>
    public bool? IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }
}
