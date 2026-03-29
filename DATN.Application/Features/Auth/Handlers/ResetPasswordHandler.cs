using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Common.Models;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Auth.Handlers;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordHandler> _logger;

    // Password policy regex: ít nhất 1 chữ hoa, 1 chữ thường, 1 số, 1 ký tự đặc biệt
    private static readonly Regex PasswordPolicyRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        RegexOptions.Compiled);

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate password policy
        if (!PasswordPolicyRegex.IsMatch(request.NewPassword))
        {
            return ApiResponse<string>.Fail(
                "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
                400, "WEAK_PASSWORD");
        }

        // Hash token raw → tìm trong DB
        var tokenHash = ComputeSha256Hash(request.Token);
        var resetToken = await _tokenRepository.GetValidTokenAsync(tokenHash, cancellationToken);

        if (resetToken == null)
        {
            _logger.LogWarning("Invalid or expired reset token used for email: {Email}", request.Email);
            return ApiResponse<string>.Fail(
                "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu link mới.",
                400, "INVALID_TOKEN");
        }

        // Validate token khớp với email/user
        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user == null || !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Reset token user mismatch. TokenUserId: {TokenUserId}, RequestEmail: {Email}", resetToken.UserId, request.Email);
            return ApiResponse<string>.Fail(
                "Link đặt lại mật khẩu không hợp lệ.",
                400, "INVALID_TOKEN");
        }

        // Hash mật khẩu mới và cập nhật user
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Mark token đã sử dụng
        await _tokenRepository.MarkAsUsedAsync(resetToken.Id, cancellationToken);

        _logger.LogInformation("Password reset successfully for UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return ApiResponse<string>.Succeed("Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(bytes);
    }
}
