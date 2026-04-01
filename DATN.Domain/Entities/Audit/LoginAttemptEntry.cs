namespace DATN.Domain.Entities.Audit;

public class LoginAttemptEntry
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public DateTime AttemptedAt { get; set; }
}
