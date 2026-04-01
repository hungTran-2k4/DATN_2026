namespace DATN.Domain.Entities.Audit;

public class UserSessionEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
