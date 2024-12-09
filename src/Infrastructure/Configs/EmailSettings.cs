using System;
namespace NejPortalBackend.Infrastructure.Configs;

public class EmailSettings
{
    public required string SmtpServer { get; set; }
    public required string WelcomeTemplatePath { get; set; }
    public required string ResetPasswordTemplatePath { get; set; }
    public required string OperationTemplatePath { get; set; }
    public int Port { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
}


