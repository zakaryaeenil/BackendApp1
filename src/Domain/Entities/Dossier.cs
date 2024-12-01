namespace NejPortalBackend.Domain.Entities;

public class Dossier : BaseAuditableEntity
{
    public required string CodeDossier { get; set; } // Unique identifier for the dossier
    public required string CodeClient { get; set; } // Name or identifier of the company
    public DateTimeOffset? Date { get; set; } // Date associated with the dossier
}
