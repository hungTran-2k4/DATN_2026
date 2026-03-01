namespace MyProject.Application.Interfaces.Services;

/// <summary>
/// Interface cho Email Service
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi email
    /// </summary>
    /// <param name="toEmail">Email người nhận</param>
    /// <param name="subject">Tiêu đề</param>
    /// <param name="htmlBody">Nội dung HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
