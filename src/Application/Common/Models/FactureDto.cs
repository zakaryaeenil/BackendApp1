using System;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;

public  class FactureDto
{
    public string? CodeFacture { get; init; } // Unique code for the facture
    public string? CodeDossier { get; init; } // Code of the associated dossier
    public DateTimeOffset? DateEcheance { get; init; } // Due date
    public DateTimeOffset? DateEmission { get; init; } // Issue date
    public decimal? MontantTotal { get; init; } // Total amount
    public decimal? MontantRestant { get; init; } // Remaining amount
    public decimal? MontantPaye { get; init; } // Paid amount
    public string? Devise { get; init; } // Currency
    public string? Description { get; init; } // Description or additional details
    public string? CheminFichier { get; init; } // File path
    public string? MethodePaiement { get; init; } // Payment method
    public string? InstructionsPaiement { get; init; } // Payment instructions
    public string? CodeClient { get; init; } // Code of the associated client
    public EtatPayement EtatPayement { get; init; } = EtatPayement.Impayée; // Payment state

    public DateTimeOffset? Created { get; init; } // Creation date
    public DateTimeOffset? LastModified { get; init; } // Last modification date

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Facture, FactureDto>()
                .ForMember(dest => dest.CodeFacture, opt => opt.MapFrom(src => src.CodeFacture))
                .ForMember(dest => dest.CodeDossier, opt => opt.MapFrom(src => src.CodeDossier))
                .ForMember(dest => dest.DateEcheance, opt => opt.MapFrom(src => src.DateEcheance))
                .ForMember(dest => dest.DateEmission, opt => opt.MapFrom(src => src.DateEmission))
                .ForMember(dest => dest.MontantTotal, opt => opt.MapFrom(src => src.MontantTotal))
                .ForMember(dest => dest.MontantRestant, opt => opt.MapFrom(src => src.MontantRestant))
                .ForMember(dest => dest.MontantPaye, opt => opt.MapFrom(src => src.MontantPaye))
                .ForMember(dest => dest.Devise, opt => opt.MapFrom(src => src.Devise))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CheminFichier, opt => opt.MapFrom(src => src.CheminFichier))
                .ForMember(dest => dest.MethodePaiement, opt => opt.MapFrom(src => src.MethodePaiement))
                .ForMember(dest => dest.InstructionsPaiement, opt => opt.MapFrom(src => src.InstructionsPaiement))
                .ForMember(dest => dest.CodeClient, opt => opt.MapFrom(src => src.CodeClient))
                .ForMember(dest => dest.EtatPayement, opt => opt.MapFrom(src => (int)src.EtatPayement))
                .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
                .ForMember(dest => dest.LastModified, opt => opt.MapFrom(src => src.LastModified));
        }
    }
}