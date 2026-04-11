using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using DATN.Application.Interfaces.Services;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Enums;
using AutoMapper;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Handlers;

/// <summary>
/// Handler cho RegisterCommand
/// </summary>
public class RegisterHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public RegisterHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<RegisterHandler> logger,
        IMapper mapper,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Kiểm tra email đã tồn tại chưa
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return ApiResponse<AuthResponse>.Fail("Email đã được sử dụng", 400, "EMAIL_ALREADY_EXISTS");
            }

            // 2. Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 3. Tạo user mới
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username : request.Email,
                PasswordHash = passwordHash,
                //FullName = request.FullName,
                AccountStatus = UserAccountStatus.Active,
                CreatedAt = DateTime.Now
            };

            await _userRepository.CreateAsync(user, cancellationToken);

            // 4. Gán default role "User"
            var defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
            if (defaultRole == null)
            {
                // Tự động tạo role User nếu chưa có (Self-seeding for convenience)
                defaultRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    Description = "Default user role"
                };
                await _roleRepository.CreateAsync(defaultRole, cancellationToken);
                _logger.LogInformation("Created default 'User' role during registration");
            }
            
            await _userRepository.AssignRoleAsync(user.Id, defaultRole.Id, cancellationToken);

            // 5. Lấy roles và tạo token
            var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetTokenExpirationMinutes());

            // Save Refresh Token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeHash(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);
            
            _logger.LogInformation("New user registered: {Email}", user.Email);

            // Gửi email chào mừng (fire-and-forget, không block response)
            _ = Task.Run(async () =>
            {
                try
                {
                    var displayName = user.FullName ?? user.Email;
                    var subject = "Chào mừng bạn đến với DATN App!";
                    var htmlBody = BuildWelcomeEmailHtml(displayName, user.Email);
                    await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send welcome email to {Email}", user.Email);
                }
            }, CancellationToken.None);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles.ToList();

            return ApiResponse<AuthResponse>.Succeed(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            }, "Đăng ký thành công", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return ApiResponse<AuthResponse>.Fail("Đã xảy ra lỗi trong quá trình đăng ký", 500, "INTERNAL_SERVER_ERROR");
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static string BuildWelcomeEmailHtml(string displayName, string email)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f7fa; font-family:Arial, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f7fa; padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:12px; box-shadow:0 2px 8px rgba(0,0,0,0.08); overflow:hidden;'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding:40px 30px; text-align:center;'>
                            <h1 style='color:#ffffff; margin:0; font-size:28px;'>🎉 Chào mừng bạn!</h1>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:40px 30px;'>
                            <h2 style='color:#333; margin-top:0;'>Xin chào {displayName},</h2>
                            <p style='color:#555; font-size:16px; line-height:1.6;'>
                                Cảm ơn bạn đã đăng ký tài khoản tại <strong>DATN App</strong>! Tài khoản của bạn đã được tạo thành công.
                            </p>
                            <table style='background-color:#f8f9fa; border-radius:8px; padding:20px; width:100%; margin:20px 0;'>
                                <tr>
                                    <td>
                                        <p style='margin:0; color:#666; font-size:14px;'>📧 <strong>Email:</strong> {email}</p>
                                    </td>
                                </tr>
                            </table>
                            <p style='color:#555; font-size:16px; line-height:1.6;'>
                                Bạn có thể đăng nhập ngay bây giờ để bắt đầu sử dụng hệ thống.
                            </p>
                            <p style='color:#999; font-size:14px; margin-top:30px;'>
                                Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email này.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8f9fa; padding:20px 30px; text-align:center; border-top:1px solid #eee;'>
                            <p style='color:#999; font-size:12px; margin:0;'>© 2026 DATN App. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
