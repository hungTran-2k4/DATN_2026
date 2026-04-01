using MediatR;
using DATN.Application.Features.Users.Queries;
using DATN.Application.Features.Users.Commands;
using DATN.Application.DTOs.Users;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class GetUserSessionsHandler : IRequestHandler<GetUserSessionsQuery, IEnumerable<UserSessionDto>>
{
    private readonly IUserSessionRepository _sessionRepository;

    public GetUserSessionsHandler(IUserSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<IEnumerable<UserSessionDto>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetActiveSessionsByUserAsync(request.UserId, cancellationToken);

        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            CreatedAt = s.CreatedAt,
            LastActivityAt = s.LastActivityAt,
            IsActive = s.RevokedAt == null
        }).ToList();
    }
}

public class RevokeUserSessionHandler : IRequestHandler<RevokeUserSessionCommand, bool>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public RevokeUserSessionHandler(IUserSessionRepository sessionRepository, IAuditLogRepository auditLogRepository)
    {
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        await _sessionRepository.RevokeSessionAsync(request.SessionId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_REVOKE_SESSION", "UserSession", request.SessionId,
            new { sessionId = request.SessionId, info = "Admin revoke single session" },
            cancellationToken: cancellationToken);

        return true;
    }
}

public class RevokeAllUserSessionsHandler : IRequestHandler<RevokeAllUserSessionsCommand, bool>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public RevokeAllUserSessionsHandler(
        IUserSessionRepository sessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository)
    {
        _sessionRepository = sessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(RevokeAllUserSessionsCommand request, CancellationToken cancellationToken)
    {
        await _sessionRepository.RevokeAllSessionsByUserAsync(request.UserId, cancellationToken);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_REVOKE_ALL_SESSIONS", "User", request.UserId,
            new { info = "Admin revoke all sessions" },
            cancellationToken: cancellationToken);

        return true;
    }
}
