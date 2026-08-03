using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHangOnline.Services;

public sealed class LocalEmailSender : IEmailSender
{
    private readonly ILogger<LocalEmailSender> _logger;

    public LocalEmailSender(ILogger<LocalEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Email sending is not configured. Subject: {Subject}, recipient: {Email}", subject, email);
        return Task.CompletedTask;
    }
}
