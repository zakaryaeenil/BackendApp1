using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;
public class CommentaireDto
{
    public int Id { get; init; }

    public string? Message { get; init; }

    public int? OperationId { get; init; }
    public string? UserId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Commentaire, CommentaireDto>()
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.Created));
        }
    }
}
