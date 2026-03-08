using System.Security.Claims;
using DATN.Domain.Entities.Identity;

namespace DATN.Application.Interfaces.Auth;

/// <summary>
/// Interface cho JWT Token Service
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Tạo access token cho user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="roles">Danh sách roles của user</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    /// <summary>
    /// Tạo refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate và decode token
    /// </summary>
    /// <param name="token">JWT token cần validate</param>
    /// <returns>ClaimsPrincipal nếu valid, null nếu invalid</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Lấy thời gian hết hạn của access token (phút)
    /// </summary>
    int GetTokenExpirationMinutes();
}
