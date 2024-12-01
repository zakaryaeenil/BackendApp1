namespace NejPortalBackend.Domain.Entities;

public class Document : BaseAuditableEntity
{
    public required string NomDocument { get; set; }
    public required int OperationId { get; set; }
    public required  bool EstAccepte { get; set; } = true;

    public required  string CheminFichier { get; set; }  // Chemin d'accès au fichier dans le système
    public long? TailleFichier { get; set; }    // Taille du fichier en octets
    public string? TypeFichier { get; set; }  // Type MIME ou extension du fichier
}
