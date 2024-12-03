using System;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Models;

public class HistoriqueDto
{

    public string? Action { get; init; }

    public int? OperationId { get; init; }
    public string? UserId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Historique, HistoriqueDto>()
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.Created));
        }
    }
}

