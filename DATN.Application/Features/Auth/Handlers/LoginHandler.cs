using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Interfaces.Auth;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;
using AutoMapper;

namespace MyProject.Application.Features.Auth.Handlers;

/// <summary>
/// Handler cho LoginCommand
/// </summary>
public class LoginHandler : IRequestHandler<Commands.LoginCommand, AuthResponse>
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

    public async Task<AuthResponse> Handle(Commands.LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không chính xác"
                };
            }

            // 2. Kiểm tra password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không chính xác"
                };
            }

            // 3. Kiểm tra user có active không
            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản đã bị vô hiệu hóa"
                };
            }

            // 4. Lấy roles của user
            var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);

            // 5. Tạo access token
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

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Đã xảy ra lỗi trong quá trình đăng nhập"
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
