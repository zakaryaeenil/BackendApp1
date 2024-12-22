using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;
public class OperationDto
{
    public int Id { get; init; }
    public string? UserId { get; init; }

    public string? Bureau { get; init; }
    public string? CodeDossier { get; init; }
    public string? Regime { get; init; }

    public bool Tr { get; set; }
    public bool Debours { get; set; }
    public bool Confirmation_Dedouanement { get; set; }
    public bool EstReserver { get; init; } 
    public string? ReserverPar { get; init; }

    public int OperationPriorite { get; set; }
    public int TypeOperation { get; init; }
    public int EtatOperation { get; init; }

    
    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    
    public bool IsLocked { get; init; } = false;
    public int nbrDocs { get; init; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Operation, OperationDto>()
                  .ForMember(d => d.Tr, opt => opt.MapFrom(s => s.TR))
                    .ForMember(d => d.Debours, opt => opt.MapFrom(s => s.DEBOURS))
                      .ForMember(d => d.Confirmation_Dedouanement, opt => opt.MapFrom(s => s.CONFIRMATION_DEDOUANEMENT))
                 .ForMember(dest => dest.OperationPriorite, opt => opt.MapFrom(src => (int)src.OperationPriorite))
                .ForMember(dest => dest.TypeOperation, opt => opt.MapFrom(src => (int)src.TypeOperation))
                .ForMember(dest => dest.EtatOperation, opt => opt.MapFrom(src => (int)src.EtatOperation))
                .ForMember(d => d.LastModified, opt => opt.MapFrom(s => s.LastModified))
                .ForMember(d => d.Created, opt => opt.MapFrom(s => s.Created))
                .ForMember(d => d.IsLocked, opt => opt.MapFrom(s => s.EtatOperation == Domain.Enums.EtatOperation.cloture))
                 .ForMember(d => d.nbrDocs, opt => opt.MapFrom(s => s.Documents.Count()));

        }
    }
}