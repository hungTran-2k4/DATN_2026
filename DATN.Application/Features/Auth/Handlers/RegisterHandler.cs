using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Auth.Commands;
using MyProject.Application.Interfaces.Auth;
using MyProject.Application.Interfaces.Roles;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;
using AutoMapper;

namespace MyProject.Application.Features.Auth.Handlers;

/// <summary>
/// Handler cho RegisterCommand
/// </summary>
public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IMapper _mapper;

    public RegisterHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<RegisterHandler> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Kiểm tra email đã tồn tại chưa
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email đã được sử dụng"
                };
            }

            // 2. Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 3. Tạo user mới
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                IsActive = true,
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

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles.ToList();

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Đã xảy ra lỗi trong quá trình đăng ký"
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
