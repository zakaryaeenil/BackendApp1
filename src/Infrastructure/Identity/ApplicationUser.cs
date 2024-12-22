using Microsoft.AspNetCore.Identity;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? CodeRef { get; set; }
    public string? Email_Notif { get; set; }
    public bool HasAccess { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    public TypeOperation? TypeOperation { get; set; } = null;

}
