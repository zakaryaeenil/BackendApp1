using System;
namespace NejPortalBackend.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendWelcomeEmailAsync(string email, string temporaryPassword, string resetPasswordLink, string userName);
}

