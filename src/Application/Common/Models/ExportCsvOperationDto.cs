using System;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Models;

public class ExportCsvOperationDto
{
    public int Id { get; init; }
    public string? UserId { get; init; }

    public string? Bureau { get; init; }
    public string? CodeDossier { get; init; }
    public string? Regime { get; init; }


    public bool EstReserver { get; init; }
    public string? ReserverPar { get; init; }


    public string? TypeOperation { get; init; }
    public string? EtatOperation { get; init; }


    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? LastModified { get; init; }

    public int nbrDocs { get; init; } = 0;
   
}

