using System.ComponentModel.DataAnnotations;

namespace DATN.Application.DTOs.Auth;

/// <summary>
/// Request DTO cho đăng nhập Firebase qua Backend (Email/Password)
/// </summary>
public class LoginWithFirebaseRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = string.Empty;
}
