using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;
using DATN.Application.Interfaces.Services;

namespace DATN.Application.Features.Users.Handlers;

public class AdminResetPasswordHandler : IRequestHandler<AdminResetPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public AdminResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        // Generate secure token
        var token = GenerateSecureToken();
        var tokenHash = ComputeHash(token);

        // Save token to DB
        var resetToken = new Domain.Entities.Identity.PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            IpAddress = "admin-triggered"
        };
        await _tokenRepository.CreateAsync(resetToken, cancellationToken);

        // Send email
        var resetLink = $"https://yourapp.com/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailAsync(
            user.Email,
            "Reset mật khẩu (Admin)",
            $"<p>Admin đã yêu cầu reset mật khẩu cho tài khoản của bạn.</p><p><a href=\"{resetLink}\">Nhấn vào đây để đặt lại mật khẩu</a></p><p>Link có hiệu lực trong 24 giờ.</p>",
            cancellationToken);

        // Audit log
        await _auditLogRepository.LogAsync(
            request.UserId, "ADMIN_RESET_PASSWORD", "User", request.UserId,
            new { info = "Admin reset password" },
            cancellationToken: cancellationToken);

        return true;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
