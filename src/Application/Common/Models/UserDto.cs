namespace NejPortalBackend.Application.Common.Models;
public class UserDto
{
    public string? Id { get; set; } // Inherited from IdentityUser
    public string? CodeRef { get; set; }
    public string? Email { get; set; } // Inherited from IdentityUser
    public bool EmailConfirmed { get; set; } // Inherited from IdentityUser
    public string? PhoneNumber { get; set; } // Inherited from IdentityUser
    public bool HasAccess { get; set; }
    public string? UserName { get; set; } // Inherited from IdentityUser
    public string? Email_Notif { get; set; }
    public string? Role { get; set; }
    public int? TypeOperation { get; set; }
}
