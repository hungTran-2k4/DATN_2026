using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class BanUserHandler : IRequestHandler<BanUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public BanUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        await _userRepository.BanUserAsync(request.UserId, cancellationToken);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_BAN_USER", "User", request.UserId,
            new { reason = request.Reason ?? "No reason provided" },
            cancellationToken: cancellationToken);

        return true;
    }
}
