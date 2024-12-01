namespace NejPortalBackend.Application.Common.Models;
public class FileInformationDto
{
    public required string NomDocument { get; init; }
    public required string CheminFichier { get; init; }
    public long? TailleFichier { get; init; }
    public string? TypeFichier { get; init; }
}