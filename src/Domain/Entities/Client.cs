namespace NejPortalBackend.Domain.Entities;

public class Client : BaseAuditableEntity
{
    public required string CodeClient { get; set; } // Unique identifier for the client
    public required string Nom { get; set; } // Name of the client
}
