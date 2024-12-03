using System;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Common.Models;

public class DossierDto
{
    public string? CodeDossier { get; set; }
    public int? NombreFactures { get; set; }
    public int? NombreOperations { get; set; }
    public string? Desription { get; set; }
    public decimal? MontantTotal { get; set; }
    public decimal? MontantPaye { get; set; }
    public decimal? MontantReste { get; set; }
    public string? Client { get; set; }
    public string? Agents { get; set; }

    public EtatPayement EtatPayement { get; set; }
}

public class DossierHelpersDto
{
    public string? CodeDossier { get; set; }
    public string? Nom { get; set; }
}
