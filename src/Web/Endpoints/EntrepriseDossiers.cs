using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierDetails;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierFilters;
using NejPortalBackend.Application.Dossiers.Queries.GetDossiers;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseDossiers : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetEntrepriseAllDossiersFilters, "filters")
        .MapGet(GetEntrepriseDossierDetails, "details/{id}")
        .MapPost(GetEntrepriseDossiersWithPagination, "all");
    }

    private async Task<DossierFiltersVm> GetEntrepriseAllDossiersFilters(ISender sender)
    {
        return await sender.Send(new GetDossierFiltersQuery());
    }
    private async Task<PaginatedList<DossierDto>> GetEntrepriseDossiersWithPagination(ISender sender, GetDossiersQuery query)
    {
        return await sender.Send(query);
    }
    private async Task<DossierDetailVm> GetEntrepriseDossierDetails(ISender sender, string id)
    {
        return await sender.Send(new GetDossierDetailsQuery { CodeDossier = id });
    }
}
