using MediatR;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Auth.Commands;
using MyProject.Application.Interfaces.Auth;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Models.Auth;
using MyProject.Domain.Entities.Identity;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;

namespace MyProject.Application.Features.Auth.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly IMapper _mapper;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        ILogger<RefreshTokenHandler> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Hash incoming token for lookup
            var tokenHash = ComputeHash(request.RefreshToken);
            _logger.LogInformation("RefreshTokenHandler: Input Token (Length: {Length}). Computed Hash: {Hash}", request.RefreshToken.Length, tokenHash);
            
            // 2. Find token in DB
            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            // 3. Validate token
            if (existingToken == null || existingToken.Revoked)
            {
                return new AuthResponse { Success = false, Message = "Invalid or expired refresh token" };
            }

            // 4. Revoke used token (Rotation)
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.Revoked = true;
            await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

            // 5. Get User
            var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return new AuthResponse { Success = false, Message = "User not found or inactive" };
            }

            // 6. Generate NEW tokens
            var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);
            var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            
            // 7. Save NEW Refresh Token
            var newTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeHash(newRefreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 days expiry
            };
            await _refreshTokenRepository.CreateAsync(newTokenEntity, cancellationToken);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles.ToList();

            return new AuthResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken, // Plain token to send back to user
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetTokenExpirationMinutes()),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse { Success = false, Message = "Error refreshing token" };
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
