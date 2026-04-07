using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Enums;
using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Auth;
using Microsoft.Extensions.Logging;

namespace DATN.Application.Features.Users.Handlers;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuditLogRepository auditLogRepository,
        ILogger<CreateUserHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Kiểm tra Email
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return ApiResponse<Guid>.Fail("Email đã được sử dụng", 400, "EMAIL_ALREADY_EXISTS");
            }

            // 2. Hash password (Do Admin tạo + skip email confirm, nên password sẽ được set luôn)
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 3. Tạo User entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                AccountStatus = UserAccountStatus.Active, // Active ngay để không phải confirm email
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user, cancellationToken);

            // 4. Gán quyền (Roles)
            if (request.RoleIds != null && request.RoleIds.Any())
            {
                foreach (var roleId in request.RoleIds)
                {
                    await _userRepository.AssignRoleAsync(user.Id, roleId, cancellationToken);
                }
            }
            else
            {
                // Gán quyền mặc định
                var defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
                if (defaultRole != null)
                {
                    await _userRepository.AssignRoleAsync(user.Id, defaultRole.Id, cancellationToken);
                }
            }

            // 5. Ghi Audit Log (Giả định ID người tạo là user mới do chưa có context admin)
            await _auditLogRepository.LogAsync(
                user.Id, "ADMIN_CREATE_USER", "User", user.Id,
                new { email = request.Email, fullName = request.FullName, rolesAssigned = request.RoleIds },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Admin created new user {Email}", user.Email);

            return ApiResponse<Guid>.Succeed(user.Id, "Tạo tài khoản thành công", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user by Admin for {Email}", request.Email);
            return ApiResponse<Guid>.Fail("Đã xảy ra lỗi trong quá trình tạo tài khoản", 500, "INTERNAL_SERVER_ERROR");
        }
    }
}
