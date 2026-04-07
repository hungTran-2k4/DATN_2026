namespace DATN.Domain.Enums;

/// <summary>
/// Trạng thái tài khoản người dùng (một nguồn sự thật; map sang cột status trong DB).
/// </summary>
public enum UserAccountStatus
{
    /// <summary>Chưa kích hoạt / chưa xác minh (ví dụ email/OTP).</summary>
    Pending = 0,

    /// <summary>Hoạt động bình thường — đăng nhập và dùng hệ thống.</summary>
    Active = 1,

    /// <summary>Khóa tạm (admin hoặc quy trình nội bộ), khác lockout sai mật khẩu (LockoutEnd).</summary>
    Locked = 2,

    /// <summary>Cấm sử dụng (vi phạm policy, gian lận, …).</summary>
    Banned = 3,

    /// <summary>Vô hiệu hóa (user tự tắt hoặc admin disable dài hạn).</summary>
    Deactivated = 4,
}
