namespace DATN.Application.DTOs.Auth;

/// <summary>
/// Response DTO trả về sau khi đăng nhập/đăng ký thành công
/// </summary>
public class AuthResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }

    /// <summary>
    /// Số lần đăng nhập còn lại trước khi bị khóa (null nếu không áp dụng)
    /// </summary>
    public int? RemainingAttempts { get; set; }

    /// <summary>
    /// Thời điểm hết khóa tài khoản (null nếu không bị khóa)
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
}

/// <summary>
/// DTO chứa thông tin user cơ bản (không bao gồm password)
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = new();
}

