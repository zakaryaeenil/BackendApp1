using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dossiers.Queries.ClientGetDossierDetails;
using NejPortalBackend.Application.Dossiers.Queries.ClientGetDossierFilters;
using NejPortalBackend.Application.Dossiers.Queries.ClientGetDossiers;

namespace NejPortalBackend.Web.Endpoints;

public class ClientDossiers : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetClientAllDossiersFilters, "filters")
        .MapGet(GetClientDossierDetails, "details/{id}")
        .MapPost(GetClientDossiersWithPagination, "all");
    }

    private async Task<DossierFiltersVm> GetClientAllDossiersFilters(ISender sender)
    {
        return await sender.Send(new ClientGetDossierFiltersQuery());
    }
    private async Task<PaginatedList<DossierDto>> GetClientDossiersWithPagination(ISender sender, ClientGetDossiersQuery query)
    {
        return await sender.Send(query);
    }
    private async Task<DossierDetailVm> GetClientDossierDetails(ISender sender, string id)
    {
        return await sender.Send(new ClientGetDossierDetailsQuery { CodeDossier = id });
    }

}