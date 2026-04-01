using MediatR;
using DATN.Application.Features.Users.Queries;
using DATN.Application.DTOs.Users;
using DATN.Application.Common.Models;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class GetUserAuditLogsHandler : IRequestHandler<GetUserAuditLogsQuery, PagedResponse<IEnumerable<AuditLogDto>>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetUserAuditLogsHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<IEnumerable<AuditLogDto>>> Handle(GetUserAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _auditLogRepository.GetPagedByUserAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(i => new AuditLogDto
        {
            Id = i.Id,
            UserId = i.UserId,
            Action = i.Action,
            TargetType = i.TargetType,
            TargetId = i.TargetId,
            Metadata = i.Metadata,
            IpAddress = i.IpAddress,
            UserAgent = i.UserAgent,
            CreatedAt = i.CreatedAt
        }).ToList();

        return PagedResponse<IEnumerable<AuditLogDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}

public class GetUserLoginHistoryHandler : IRequestHandler<GetUserLoginHistoryQuery, PagedResponse<IEnumerable<LoginAttemptDto>>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetUserLoginHistoryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<IEnumerable<LoginAttemptDto>>> Handle(GetUserLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _auditLogRepository.GetLoginAttemptsAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(i => new LoginAttemptDto
        {
            Id = i.Id,
            Email = i.Email,
            IpAddress = i.IpAddress,
            Success = i.Success,
            AttemptedAt = i.AttemptedAt
        }).ToList();

        return PagedResponse<IEnumerable<LoginAttemptDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}
