using System.ComponentModel.DataAnnotations;

namespace DATN.Application.DTOs.Auth;

/// <summary>
/// Request DTO cho đăng ký tài khoản qua Firebase (Email/Password)
/// </summary>
public class RegisterWithFirebaseRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    public string FullName { get; set; } = string.Empty;
}
