using System;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Models;

public class ClientDto
{
    public string? CodeClient { get; init; }

    public string? Nom { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Client, ClientDto>()
            .ForMember(d => d.CodeClient, opt => opt.MapFrom(s => s.CodeClient))
             .ForMember(d => d.Nom, opt => opt.MapFrom(s => s.Nom));
        }
    }
}


