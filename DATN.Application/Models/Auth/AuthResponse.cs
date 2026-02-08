namespace MyProject.Application.Models.Auth;

/// <summary>
/// Response DTO trả về sau khi đăng nhập/đăng ký thành công
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// DTO chứa thông tin user cơ bản (không bao gồm password)
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public List<string> Roles { get; set; } = new();
}
