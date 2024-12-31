using System;
using Microsoft.Extensions.Options;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Infrastructure.Configs;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;

namespace NejPortalBackend.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IWebHostEnvironment _environment;
    public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment environment)
    {
        _emailSettings = emailSettings.Value;
        _environment = environment;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        using var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
        {
            Port = _emailSettings.Port,
            Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        mailMessage.To.Add(to);

        await smtpClient.SendMailAsync(mailMessage);
    }

    public async Task SendWelcomeEmailAsync(string email, string temporaryPassword, string resetPasswordLink, string userName)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, _emailSettings.WelcomeTemplatePath);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found at {templatePath}");
        }

        // Read and process the email template
        var emailTemplate = await File.ReadAllTextAsync(templatePath);

        emailTemplate = emailTemplate
            .Replace("{{UserName}}", userName)
            .Replace("{{Email}}", email)
            .Replace("{{TemporaryPassword}}", temporaryPassword)
            .Replace("{{ResetPasswordLink}}", resetPasswordLink);

        // Send the email
        await SendEmailAsync(email, "Welcome to NejPortal", emailTemplate);
    }
    public async Task SendResetPasswordEmailAsync(string email, string resetPasswordLink, string userName)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, _emailSettings.ResetPasswordTemplatePath);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found at {templatePath}");
        }

        // Read and process the email template
        var emailTemplate = await File.ReadAllTextAsync(templatePath);

        emailTemplate = emailTemplate
            .Replace("{{UserName}}", userName)
            .Replace("{{UserEmail}}", email)
            .Replace("{{ResetPasswordLink}}", resetPasswordLink);

        // Send the email
        await SendEmailAsync(email, "Nej Portal", emailTemplate);
    }
    public async Task SendOperationEmailAsync(string email, int operationId, string message, string userName)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, _emailSettings.OperationTemplatePath);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found at {templatePath}");
        }

        // Read and process the email template
        var emailTemplate = await File.ReadAllTextAsync(templatePath);

        emailTemplate = emailTemplate
            .Replace("{{UserName}}", userName)
            .Replace("{{OperationId}}", operationId.ToString())
            .Replace("{{Message}}", message);

        // Send the email
        await SendEmailAsync(email, "Nej Portal", emailTemplate);
    }

}

