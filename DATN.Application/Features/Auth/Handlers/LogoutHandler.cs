using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Features.Auth.Commands;
using DATN.Domain.Interfaces;
using DATN.Application.Common.Models;
using System.Security.Cryptography;
using System.Text;

namespace DATN.Application.Features.Auth.Handlers;

public class LogoutHandler : IRequestHandler<LogoutCommand, ApiResponse<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return ApiResponse<bool>.Succeed(true, "Đăng xuất thành công");
            }

            var tokenHash = ComputeHash(request.RefreshToken);
            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (existingToken != null && !existingToken.Revoked)
            {
                existingToken.RevokedAt = DateTime.UtcNow;
                existingToken.Revoked = true;
                await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);
                _logger.LogInformation("Refresh token revoked during logout for User {UserId}", existingToken.UserId);
            }

            return ApiResponse<bool>.Succeed(true, "Đăng xuất thành công", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Vẫn trả về true (thành công ở phía client)
            return ApiResponse<bool>.Succeed(true, "Đăng xuất thành công", 200);
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
