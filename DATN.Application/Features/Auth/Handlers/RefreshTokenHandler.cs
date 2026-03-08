using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Entities.Identity;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;

namespace DATN.Application.Features.Auth.Handlers;

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
            _logger.LogInformation(
                "Token validation - Id: {Id}, Revoked: {Revoked}, RevokedAt: {RevokedAt}, ReplaceByTokenId: {ReplaceBy}, ExpiresAt: {ExpiresAt}",
                existingToken?.Id, existingToken?.Revoked, existingToken?.RevokedAt, existingToken?.ReplaceByTokenId, existingToken?.ExpiresAt);
            
            if (existingToken == null || existingToken.Revoked || existingToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token rejected! Null: {IsNull}, Revoked: {Revoked}, Expired: {Expired}", 
                    existingToken == null, existingToken?.Revoked, existingToken?.ExpiresAt < DateTime.UtcNow);
                return new AuthResponse { Success = false, Message = "Invalid or expired refresh token" };
            }

            // 4. Phát hiện token đã bị rotate trước đó (reuse detection)
            if (existingToken.ReplaceByTokenId != null)
            {
                // Token này đã được dùng rồi → có thể bị đánh cắp
                // Revoke TẤT CẢ token của user để bảo mật
                _logger.LogWarning("Refresh token reuse detected for user {UserId}! Revoking all tokens.", existingToken.UserId);
                await _refreshTokenRepository.RevokeAllByUserIdAsync(existingToken.UserId, cancellationToken);
                return new AuthResponse { Success = false, Message = "Token đã được sử dụng. Vui lòng đăng nhập lại." };
            }

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
            
            // 7. Tạo token mới
            var newTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeHash(newRefreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            // 8. Tạo token mới TRƯỚC (vì ReplaceByTokenId là FK trỏ đến token mới, token mới phải tồn tại trước)
            await _refreshTokenRepository.CreateAsync(newTokenEntity, cancellationToken);

            // 9. Sau khi token mới đã tồn tại, revoke token cũ và trỏ đến token mới
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.Revoked = true;
            existingToken.ReplaceByTokenId = newTokenEntity.Id;
            await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

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
