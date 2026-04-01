using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class UnlockUserHandler : IRequestHandler<UnlockUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UnlockUserHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        await _userRepository.UnlockUserAsync(request.UserId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_UNLOCK_USER", "User", request.UserId,
            new { info = "Admin unlock" },
            cancellationToken: cancellationToken);

        return true;
    }
}
