using DATN.Domain.Enums;

namespace DATN.Domain.Entities.Identity;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>Trạng thái tài khoản (map cột users.status).</summary>
    public UserAccountStatus AccountStatus { get; set; } = UserAccountStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Lockout properties (sai mật khẩu — tách biệt với AccountStatus.Locked)
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
