using FirebaseAdmin.Auth;
using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Auth.Commands;
using MyProject.Application.Interfaces.Roles;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;

namespace MyProject.Application.Features.Auth.Handlers;

public class RegisterWithFirebaseHandler : IRequestHandler<RegisterWithFirebaseCommand, AuthResponse>
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

    public async Task<AuthResponse> Handle(RegisterWithFirebaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if user exists locally
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email đã tồn tại trong hệ thống"
                };
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
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Lỗi tạo tài khoản Firebase: {ex.Message}"
                };
            }

            // 3. Create User in Local DB
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                IsActive = true,
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

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công",
                // We don't return tokens here because the client typically needs to login 
                // to get the ID Token from Firebase Client SDK, then exchange it.
                // Or we could login immediately if we had the ID Token, but we only have Uid/Email here from Admin SDK.
                // So returning success is enough. The client can then call LoginWithFirebase using the credentials they just used.
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Firebase registration");
            return new AuthResponse
            {
                Success = false,
                Message = "Đã xảy ra lỗi hệ thống"
            };
        }
    }
}
