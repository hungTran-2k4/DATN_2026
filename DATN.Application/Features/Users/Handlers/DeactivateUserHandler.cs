using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeactivateUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        await _userRepository.DeactivateUserAsync(request.UserId, cancellationToken);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_DEACTIVATE_USER", "User", request.UserId,
            new { reason = request.Reason ?? "No reason provided" },
            cancellationToken: cancellationToken);

        return true;
    }
}

public class ActivateUserHandler : IRequestHandler<ActivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ActivateUserHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        await _userRepository.ActivateUserAsync(request.UserId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_ACTIVATE_USER", "User", request.UserId,
            new { info = "Admin activation" },
            cancellationToken: cancellationToken);

        return true;
    }
}
