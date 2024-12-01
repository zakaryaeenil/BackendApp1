namespace NejPortalBackend.Domain.Entities;

public class Facture : BaseAuditableEntity
{
    public  int? Indice { get; set; } // Unique identifier or index of the facture
    public  string? CodeFacture { get; set; } // Unique code for the facture
    public string? CodeDossier { get; set; } // Code of the associated dossier
    public DateTimeOffset? DateEcheance { get; set; } // Due date of the facture
    public DateTimeOffset? DateEmission { get; set; } // Issue date of the facture
    public decimal? MontantTotal { get; set; } // Total amount of the facture
    public decimal? MontantRestant { get; set; } // Remaining amount to be paid
    public decimal? MontantPaye { get; set; } // Amount already paid
    public string? Devise { get; set; } // Currency of the facture
    public string? Description { get; set; } // Description or additional details
    public string? CheminFichier { get; set; } // File path for the facture
    public string? MethodePaiement { get; set; } // Payment method
    public string? InstructionsPaiement { get; set; } // Payment instructions
    public string? CodeClient { get; set; } // Code of the associated client

    public EtatPayement EtatPayement { get; set; } = EtatPayement.Impayée;
}
