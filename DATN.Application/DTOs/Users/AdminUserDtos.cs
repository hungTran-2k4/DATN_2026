namespace DATN.Application.DTOs.Users;

public class UserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "active";
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoginAttemptDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public DateTime AttemptedAt { get; set; }
}

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}

public class LockUserRequest
{
    public string? Reason { get; set; }
}

public class AdminUpdateUserRequest
{
    public string? FullName { get; set; }
}
