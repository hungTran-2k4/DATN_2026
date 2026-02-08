using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Auth.Commands;
using MyProject.Application.Interfaces.Auth;
using MyProject.Application.Interfaces.Roles;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;
using System.Net.Http.Json;

namespace MyProject.Application.Features.Auth.Handlers;

public class LoginWithFirebaseHandler : IRequestHandler<LoginWithFirebaseCommand, AuthResponse>
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

    public async Task<AuthResponse> Handle(LoginWithFirebaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Call Firebase REST API to sign in with password
            var apiKey = _configuration["Firebase:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return new AuthResponse { Success = false, Message = "Server configuration error: Firebase ApiKey missing" };
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
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không chính xác"
                };
            }

            var paramsResult = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
            
            if (paramsResult == null) // checks paramsResult (using variable name since I can't use 'result' if it conflicts, but here it is uniquely named or I can rely on var name)
            {
                 return new AuthResponse { Success = false, Message = "Lỗi xác thực từ Firebase" };
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

             if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản đã bị vô hiệu hóa"
                };
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

            return new AuthResponse
            {
                Success = true,
                Message = isNewUser ? "Đăng ký và đăng nhập thành công" : "Đăng nhập thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Firebase login");
            return new AuthResponse
            {
                Success = false,
                Message = "Đã xảy ra lỗi hệ thống"
            };
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
