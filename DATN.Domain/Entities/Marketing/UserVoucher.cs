namespace DATN.Domain.Entities.Marketing;

public class UserVoucher
{
    public Guid UserId { get; set; }
    public Guid VoucherId { get; set; }
    public bool? IsUsed { get; set; } = false;
    public DateTime? SavedAt { get; set; }
}
