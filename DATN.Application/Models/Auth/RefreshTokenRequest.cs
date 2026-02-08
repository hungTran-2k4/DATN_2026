using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.Models.Auth;

/// <summary>
/// Request DTO để refresh access token
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "RefreshToken là bắt buộc")]
    public string RefreshToken { get; set; } = string.Empty;
}
