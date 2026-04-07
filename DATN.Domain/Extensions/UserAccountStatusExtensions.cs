using DATN.Domain.Enums;

namespace DATN.Domain.Extensions;

/// <summary>
/// Map giữa enum và chuỗi lưu trong DB (hỗ trợ giá trị legacy: active, inactive, locked, deactivated).
/// </summary>
public static class UserAccountStatusExtensions
{
    /// <summary>Chuỗi lưu DB/API (chữ thường, ổn định với dữ liệu cũ).</summary>
    public static string ToDatabaseString(this UserAccountStatus status) =>
        status switch
        {
            UserAccountStatus.Pending => "pending",
            UserAccountStatus.Active => "active",
            UserAccountStatus.Locked => "locked",
            UserAccountStatus.Banned => "banned",
            UserAccountStatus.Deactivated => "deactivated",
            _ => "active"
        };

    public static UserAccountStatus FromDatabaseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return UserAccountStatus.Active;

        var v = value.Trim().ToLowerInvariant();
        return v switch
        {
            "active" => UserAccountStatus.Active,
            "pending" => UserAccountStatus.Pending,
            "inactive" => UserAccountStatus.Pending, // legacy: coi như chưa kích hoạt
            "locked" => UserAccountStatus.Locked,
            "banned" => UserAccountStatus.Banned,
            "deactivated" => UserAccountStatus.Deactivated,
            _ when v == UserAccountStatus.Active.ToString().ToLowerInvariant() => UserAccountStatus.Active,
            _ when v == UserAccountStatus.Pending.ToString().ToLowerInvariant() => UserAccountStatus.Pending,
            _ when v == UserAccountStatus.Locked.ToString().ToLowerInvariant() => UserAccountStatus.Locked,
            _ when v == UserAccountStatus.Banned.ToString().ToLowerInvariant() => UserAccountStatus.Banned,
            _ when v == UserAccountStatus.Deactivated.ToString().ToLowerInvariant() => UserAccountStatus.Deactivated,
            _ => UserAccountStatus.Active
        };
    }

    /// <summary>Chỉ ACTIVE mới được cấp session đầy đủ (JWT/refresh).</summary>
    public static bool AllowsFullSession(this UserAccountStatus status) =>
        status == UserAccountStatus.Active;

    /// <summary>Lý do từ chối đăng nhập khi không phải lockout mật khẩu (LockoutEnd).</summary>
    public static (string ErrorCode, string Message)? GetLoginDenial(this UserAccountStatus status) =>
        status switch
        {
            UserAccountStatus.Active => null,
            UserAccountStatus.Pending => (
                "ACCOUNT_PENDING",
                "Vui lòng xác minh tài khoản (email/OTP) trước khi đăng nhập."),
            UserAccountStatus.Locked => (
                "ACCOUNT_LOCKED_BY_ADMIN",
                "Tài khoản đã bị khóa bởi quản trị viên. Vui lòng liên hệ hỗ trợ."),
            UserAccountStatus.Banned => (
                "ACCOUNT_BANNED",
                "Tài khoản đã bị cấm sử dụng hệ thống."),
            UserAccountStatus.Deactivated => (
                "ACCOUNT_DEACTIVATED",
                "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ hỗ trợ."),
            _ => ("ACCOUNT_DISABLED", "Tài khoản không thể đăng nhập.")
        };
}
