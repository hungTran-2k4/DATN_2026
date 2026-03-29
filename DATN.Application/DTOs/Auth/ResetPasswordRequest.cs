using System.ComponentModel.DataAnnotations;

namespace DATN.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải dài ít nhất 8 ký tự")]
    public string NewPassword { get; set; } = string.Empty;
}
