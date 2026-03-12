using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using System.Net.Http.Json;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Auth.Handlers;

public class LoginWithFirebaseHandler : IRequestHandler<LoginWithFirebaseCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginWithFirebaseHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public LoginWithFirebaseHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginWithFirebaseHandler> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient(); // In real app, inject via IHttpClientFactory
    }

    // Hằng số cấu hình lockout
    private const int MAX_ATTEMPTS = 3;
    private const int LOCKOUT_MINUTES = 5;

    public async Task<ApiResponse<AuthResponse>> Handle(LoginWithFirebaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Call Firebase REST API to sign in with password
            var apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return ApiResponse<AuthResponse>.Fail("Server configuration error: Firebase ApiKey missing", 500, "CONFIG_ERROR");
            }

            var firebaseLoginUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
            
            var loginPayload = new
            {
                email = request.Email,
                password = request.Password,
                returnSecureToken = true
            };

            var response = await _httpClient.PostAsJsonAsync(firebaseLoginUrl, loginPayload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Firebase Login Failed: {Error}", errorContent);

                // Kiểm tra user trong DB để tăng đếm lần sai
                var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                if (existingUser != null)
                {
                    // Kiểm tra lockout trước
                    if (existingUser.LockoutEnd.HasValue && existingUser.LockoutEnd.Value > DateTime.UtcNow)
                    {
                        var remainingTime = existingUser.LockoutEnd.Value - DateTime.UtcNow;
                        return new ApiResponse<AuthResponse>
                        {
                            Success = false,
                            StatusCode = 403,
                            ErrorCode = "ACCOUNT_LOCKED",
                            Message = $"Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {Math.Ceiling(remainingTime.TotalMinutes)} phút.",
                            Data = new AuthResponse
                            {
                                LockoutEnd = existingUser.LockoutEnd.Value,
                                RemainingAttempts = 0
                            }
                        };
                    }

                    // Reset nếu lockout đã hết hạn
                    if (existingUser.LockoutEnd.HasValue && existingUser.LockoutEnd.Value <= DateTime.UtcNow)
                    {
                        await _userRepository.ResetFailedLoginAsync(existingUser.Id, cancellationToken);
                        existingUser.FailedLoginCount = 0;
                    }

                    await _userRepository.IncrementFailedLoginAsync(existingUser.Id, MAX_ATTEMPTS, LOCKOUT_MINUTES, cancellationToken);
                    var newFailedCount = existingUser.FailedLoginCount + 1;
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

                return ApiResponse<AuthResponse>.Fail("Email hoặc mật khẩu không chính xác", 401, "INVALID_CREDENTIALS");
            }

            var paramsResult = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
            
            if (paramsResult == null)
            {
                 return ApiResponse<AuthResponse>.Fail("Lỗi xác thực từ Firebase", 401, "FIREBASE_AUTH_FAILED");
            }

            string email = paramsResult.email;
            string uid = paramsResult.localId;

            // 2. Check if user exists
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            bool isNewUser = false;

            if (user == null)
            {
                // 3. Auto-register new user
                isNewUser = true;
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = paramsResult.displayName ?? email.Split('@')[0],
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString()) 
                };

                await _userRepository.CreateAsync(user, cancellationToken);
                
                // Add default "User" role
                var userRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
                if (userRole != null)
                {
                    await _userRepository.AssignRoleAsync(user.Id, userRole.Id, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Default role 'User' not found. User {UserId} auto-created without role.", user.Id);
                }
            }

            // Kiểm tra lockout cho user đã tồn tại
            if (!isNewUser)
            {
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
            }

             if (!user.IsActive)
            {
                return ApiResponse<AuthResponse>.Fail("Tài khoản đã bị vô hiệu hóa", 403, "ACCOUNT_DISABLED");
            }

            // Đăng nhập thành công → Reset bộ đếm sai
            if (user.FailedLoginCount > 0)
            {
                await _userRepository.ResetFailedLoginAsync(user.Id, cancellationToken);
            }

            // 4. Get Roles
            var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);

            // 5. Generate Local Tokens
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetTokenExpirationMinutes());

            // 6. Save Refresh Token
             var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeHash(refreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

            _logger.LogInformation("User {Email} logged in via Firebase Endpoint", user.Email);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles.ToList()
            };

            return ApiResponse<AuthResponse>.Succeed(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            }, isNewUser ? "Đăng ký và đăng nhập thành công" : "Đăng nhập thành công", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Firebase login");
            return ApiResponse<AuthResponse>.Fail("Đã xảy ra lỗi hệ thống", 500, "INTERNAL_SERVER_ERROR");
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

public class FirebaseSignInResponse 
{
    public string localId { get; set; }
    public string email { get; set; }
    public string displayName { get; set; }
    public string idToken { get; set; }
    public bool registered { get; set; }
    public string refreshToken { get; set; }
    public string expiresIn { get; set; }
}
