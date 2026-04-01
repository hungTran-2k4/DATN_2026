using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using AutoMapper;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Handlers;

/// <summary>
/// Handler cho LoginCommand
/// </summary>
public class LoginHandler : IRequestHandler<Commands.LoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IMapper _mapper;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginHandler> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _mapper = mapper;
    }

    // Hằng số cấu hình lockout
    private const int MAX_ATTEMPTS = 3;
    private const int LOCKOUT_MINUTES = 5;

    public async Task<ApiResponse<AuthResponse>> Handle(Commands.LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            
            if (user == null)
            {
                return ApiResponse<AuthResponse>.Fail("Email hoặc mật khẩu không chính xác", 401, "INVALID_CREDENTIALS");
            }

            // 2. Kiểm tra tài khoản có đang bị khóa không
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingTime = user.LockoutEnd.Value - DateTime.UtcNow;
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    StatusCode = 403,
                    ErrorCode = "ACCOUNT_LOCKED",
                    Message = $"Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {Math.Ceiling(remainingTime.TotalMinutes)} phút.",
                    Data = new AuthResponse
                    {
                        LockoutEnd = user.LockoutEnd.Value,
                        RemainingAttempts = 0
                    }
                };
            }

            // 3. Nếu lockout đã hết hạn, reset bộ đếm
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow)
            {
                await _userRepository.ResetFailedLoginAsync(user.Id, cancellationToken);
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
            }

            // 3.5 Kiểm tra trạng thái tài khoản (locked bởi admin, deactivated)
            var userStatus = await _userRepository.GetUserStatusAsync(user.Id, cancellationToken);
            if (userStatus == "locked")
            {
                return ApiResponse<AuthResponse>.Fail(
                    "Tài khoản đã bị khóa bởi quản trị viên. Vui lòng liên hệ hỗ trợ.",
                    403, "ACCOUNT_LOCKED_BY_ADMIN");
            }
            if (userStatus == "deactivated")
            {
                return ApiResponse<AuthResponse>.Fail(
                    "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ hỗ trợ.",
                    403, "ACCOUNT_DEACTIVATED");
            }

            // 4. Kiểm tra password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Tăng số lần đăng nhập sai
                await _userRepository.IncrementFailedLoginAsync(user.Id, MAX_ATTEMPTS, LOCKOUT_MINUTES, cancellationToken);
                var newFailedCount = user.FailedLoginCount + 1;
                var remaining = MAX_ATTEMPTS - newFailedCount;

                if (remaining <= 0)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        StatusCode = 403,
                        ErrorCode = "ACCOUNT_LOCKED",
                        Message = $"Đăng nhập sai quá {MAX_ATTEMPTS} lần. Tài khoản bị khóa tạm thời {LOCKOUT_MINUTES} phút.",
                        Data = new AuthResponse
                        {
                            RemainingAttempts = 0,
                            LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES)
                        }
                    };
                }

                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    StatusCode = 401,
                    ErrorCode = "INVALID_CREDENTIALS",
                    Message = $"Email hoặc mật khẩu không chính xác. Còn {remaining} lần thử.",
                    Data = new AuthResponse
                    {
                        RemainingAttempts = remaining
                    }
                };
            }

            // 5. Kiểm tra user có active không
            if (!user.IsActive)
            {
                return ApiResponse<AuthResponse>.Fail("Tài khoản đã bị vô hiệu hóa", 403, "ACCOUNT_DISABLED");
            }

            // 6. Đăng nhập thành công → Reset bộ đếm sai
            if (user.FailedLoginCount > 0)
            {
                await _userRepository.ResetFailedLoginAsync(user.Id, cancellationToken);
            }

            // 7. Lấy roles của user
            var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);

            // 8. Tạo access token
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
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 days expiry
            };
            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);
            
            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles.ToList();

            return ApiResponse<AuthResponse>.Succeed(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            }, "Đăng nhập thành công", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return ApiResponse<AuthResponse>.Fail("Đã xảy ra lỗi trong quá trình đăng nhập", 500, "INTERNAL_SERVER_ERROR");
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
