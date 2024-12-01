using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Models;


public class DocumentDto
{
    public int Id { get; init; }

    public string NomDocument { get; init; } = default!;

    public int OperationId { get; init; }

    public bool EstAccepte { get; init; } = true;

    public string CheminFichier { get; init; } = default!;

    public long? TailleFichier { get; init; }

    public string? TypeFichier { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Document, DocumentDto>();
        }
    }
}