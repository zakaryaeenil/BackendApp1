using NejPortalBackend.Domain.Entities;
namespace NejPortalBackend.Application.Common.Models;
public class OperationDto
{
    public int Id { get; init; }
    public string? UserId { get; init; }

    public string? Bureau { get; init; }
    public string? CodeDossier { get; init; }
    public string? Regime { get; init; }

    
    public bool EstReserver { get; init; } 
    public string? ReserverPar { get; init; }

    
    public int TypeOperation { get; init; }
    public int EtatOperation { get; init; }

    
    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    
    public bool IsLocked { get; init; } = false;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Operation, OperationDto>()
                .ForMember(dest => dest.TypeOperation, opt => opt.MapFrom(src => (int)src.TypeOperation))
                .ForMember(dest => dest.EtatOperation, opt => opt.MapFrom(src => (int)src.EtatOperation))
                .ForMember(d => d.LastModified, opt => opt.MapFrom(s => s.LastModified))
                .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created))
                .ForMember(d => d.IsLocked, opt => opt.MapFrom(s => s.EtatOperation == Domain.Enums.EtatOperation.cloture));
        }
    }
}