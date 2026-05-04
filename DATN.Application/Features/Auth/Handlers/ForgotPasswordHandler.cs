using MediatR;
using Microsoft.Extensions.Logging;
using DATN.Application.Common.Models;
using DATN.Application.Features.Auth.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Application.Interfaces.Auth;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Auth.Handlers;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    private const string DefaultPassword = "abc@1234";
    private const string GenericMessage = "Nếu email tồn tại, chúng tôi đã gửi mật khẩu mới về hòm thư của bạn.";

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Forgot password requested for non-existent email: {Email}", request.Email);
            return ApiResponse<string>.Succeed(GenericMessage);
        }

        // 1. Reset mật khẩu về mặc định
        user.PasswordHash = _passwordHasher.HashPassword(DefaultPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 2. Gửi email thông báo
        var htmlBody = $@"
            <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e1e4e8; border-radius: 12px; background-color: #ffffff;"">
                <div style=""text-align: center; margin-bottom: 30px;"">
                    <h1 style=""color: #1a73e8; margin: 0; font-size: 24px;"">Khôi phục mật khẩu thành công</h1>
                </div>
                
                <p style=""color: #3c4043; font-size: 16px; line-height: 1.6;"">Xin chào,</p>
                <p style=""color: #3c4043; font-size: 16px; line-height: 1.6;"">Chúng tôi đã đặt lại mật khẩu cho tài khoản của bạn theo yêu cầu. Dưới đây là thông tin đăng nhập tạm thời:</p>
                
                <div style=""background-color: #f8f9fa; border-left: 4px solid #1a73e8; padding: 20px; margin: 25px 0; border-radius: 4px;"">
                    <p style=""margin: 0; color: #5f6368; font-size: 14px;"">Mật khẩu mới của bạn là:</p>
                    <p style=""margin: 10px 0 0 0; color: #202124; font-size: 20px; font-weight: bold; letter-spacing: 1px;"">{DefaultPassword}</p>
                </div>

                <div style=""background-color: #fff3cd; border: 1px solid #ffeeba; padding: 15px; border-radius: 8px; margin-bottom: 25px;"">
                    <p style=""margin: 0; color: #856404; font-size: 14px; font-weight: bold;"">⚠️ CẢNH BÁO BẢO MẬT:</p>
                    <p style=""margin: 5px 0 0 0; color: #856404; font-size: 14px;"">Vì lý do an toàn, vui lòng <strong>đổi lại mật khẩu ngay lập tức</strong> sau khi đăng nhập thành công.</p>
                </div>

                <p style=""color: #5f6368; font-size: 14px; line-height: 1.5;"">Nếu bạn không yêu cầu thay đổi này, hãy liên hệ ngay với bộ phận hỗ trợ của chúng tôi để được giúp đỡ.</p>
                
                <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"" />
                <p style=""color: #999; font-size: 12px; text-align: center;"">Đây là email tự động, vui lòng không trả lời email này.<br>© 2026 DATN App Team</p>
            </div>";

        await _emailService.SendEmailAsync(request.Email, "Thông tin khôi phục mật khẩu - DATN App", htmlBody, cancellationToken);

        _logger.LogInformation("Password reset to default and email sent to {Email}, UserId: {UserId}", request.Email, user.Id);

        return ApiResponse<string>.Succeed(GenericMessage);
    }
}
