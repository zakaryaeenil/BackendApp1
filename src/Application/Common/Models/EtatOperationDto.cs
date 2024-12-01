using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;
public class EtatOperationDto
{
    public int Value { get; init; }

    public string? Name { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<EtatOperation, EtatOperationDto>();
        }
    }
}
