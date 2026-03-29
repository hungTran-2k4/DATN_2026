using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DATN.Application.Common.Models;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Auth.Handlers;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    private const int MaxRequestsPerDay = 3;
    private const int TokenExpiryMinutes = 15;
    private const string GenericMessage = "Nếu email tồn tại, chúng tôi đã gửi link đặt lại mật khẩu.";

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Luôn trả cùng 1 message để không leak thông tin email tồn tại hay không
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Forgot password requested for non-existent email: {Email}", request.Email);
            return ApiResponse<string>.Succeed(GenericMessage);
        }

        // Rate limit: max 3 lần/ngày/user
        var todayCount = await _tokenRepository.CountTodayByUserAsync(user.Id, cancellationToken);
        if (todayCount >= MaxRequestsPerDay)
        {
            _logger.LogWarning("Rate limit exceeded for forgot password. UserId: {UserId}, Count: {Count}", user.Id, todayCount);
            return ApiResponse<string>.Succeed(GenericMessage);
        }

        // Invalidate tất cả token cũ của user
        await _tokenRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

        // Generate token bảo mật: 32 bytes random → Base64 URL-safe
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Hash token bằng SHA256 trước khi lưu DB
        var tokenHash = ComputeSha256Hash(rawToken);

        // Lưu vào DB
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress
        };

        await _tokenRepository.CreateAsync(resetToken, cancellationToken);

        // Gửi email chứa link reset (rawToken chỉ gửi qua email, DB chỉ lưu hash)
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetLink = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(request.Email)}";

        var htmlBody = $@"
            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
                <h2 style=""color: #333;"">Đặt lại mật khẩu</h2>
                <p>Xin chào,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                <p>Nhấn vào nút bên dưới để đặt mật khẩu mới:</p>
                <div style=""text-align: center; margin: 30px 0;"">
                    <a href=""{resetLink}"" 
                       style=""background-color: #4F46E5; color: white; padding: 12px 24px; 
                              text-decoration: none; border-radius: 6px; font-weight: bold;"">
                        Đặt lại mật khẩu
                    </a>
                </div>
                <p style=""color: #666; font-size: 14px;"">Link này sẽ hết hạn sau {TokenExpiryMinutes} phút.</p>
                <p style=""color: #666; font-size: 14px;"">Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                <hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;"" />
                <p style=""color: #999; font-size: 12px;"">Email này được gửi tự động, vui lòng không trả lời.</p>
            </div>";

        await _emailService.SendEmailAsync(request.Email, "Đặt lại mật khẩu", htmlBody, cancellationToken);

        _logger.LogInformation("Password reset email sent to {Email}, UserId: {UserId}", request.Email, user.Id);

        return ApiResponse<string>.Succeed(GenericMessage);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(bytes);
    }
}
