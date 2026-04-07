using MediatR;

namespace DATN.Application.Features.Users.Commands;

/// <summary>Admin cấm tài khoản (policy / gian lận) — trạng thái Banned.</summary>
public class BanUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? Reason { get; set; }

    public BanUserCommand(Guid userId, string? reason = null)
    {
        UserId = userId;
        Reason = reason;
    }
}
