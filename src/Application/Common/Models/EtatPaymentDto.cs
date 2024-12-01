using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;


public class EtatPaymentDto
{
    public int Value { get; init; }

    public string? Name { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<EtatPayement, EtatPaymentDto>();
        }
    }
}