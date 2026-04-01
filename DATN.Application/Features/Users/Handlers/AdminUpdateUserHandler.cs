using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class AdminUpdateUserHandler : IRequestHandler<AdminUpdateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public AdminUpdateUserHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_UPDATE_USER", "User", request.UserId,
            new { fullName = request.FullName, info = "Admin update" },
            cancellationToken: cancellationToken);

        return true;
    }
}
