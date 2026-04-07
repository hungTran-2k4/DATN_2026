using FirebaseAdmin.Auth;
using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Enums;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Handlers;

public class RegisterWithFirebaseHandler : IRequestHandler<RegisterWithFirebaseCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RegisterWithFirebaseHandler> _logger;

    public RegisterWithFirebaseHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<RegisterWithFirebaseHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RegisterWithFirebaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if user exists locally
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return ApiResponse<AuthResponse>.Fail("Email đã tồn tại trong hệ thống", 400, "EMAIL_ALREADY_EXISTS");
            }

            // 2. Create User in Firebase
            UserRecord userRecord;
            try
            {
                var userArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.FullName,
                    EmailVerified = false,
                    Disabled = false
                };
                userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs, cancellationToken);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(ex, "Firebase CreateUser failed");
                return ApiResponse<AuthResponse>.Fail($"Lỗi tạo tài khoản Firebase: {ex.Message}", 400, "FIREBASE_ERROR");
            }

            // 3. Create User in Local DB
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                AccountStatus = UserAccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = "FirebaseUser" // Placeholder, not used for authentication
            };

            await _userRepository.CreateAsync(user, cancellationToken);

            // 4. Assign "User" Role
            var userRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
            if (userRole != null)
            {
                await _userRepository.AssignRoleAsync(user.Id, userRole.Id, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Default role 'User' not found. User {UserId} created without role.", user.Id);
            }

            _logger.LogInformation("User {Email} registered successfully via Firebase", user.Email);

            return ApiResponse<AuthResponse>.Succeed(new AuthResponse(), "Đăng ký thành công", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Firebase registration");
            return ApiResponse<AuthResponse>.Fail("Đã xảy ra lỗi hệ thống", 500, "INTERNAL_SERVER_ERROR");
        }
    }
}
