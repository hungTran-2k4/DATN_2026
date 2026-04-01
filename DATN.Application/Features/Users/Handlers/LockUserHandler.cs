using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class LockUserHandler : IRequestHandler<LockUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public LockUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        // Lock user
        await _userRepository.LockUserAsync(request.UserId, cancellationToken);

        // Revoke all refresh tokens (force logout)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        // Audit log
        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_LOCK_USER", "User", request.UserId,
            new { reason = request.Reason ?? "No reason provided" },
            cancellationToken: cancellationToken);

        return true;
    }
}
