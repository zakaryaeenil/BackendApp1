using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dashboard.Queries.ClientGetDashboard;
using NejPortalBackend.Application.Historiques.Queries.GetHistoriques;
using NejPortalBackend.Application.Operations.Queries.GetOperationFilters;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseHistoriques : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetHistoriques, "all");
    }
    public async Task<IList<HistoriqueDto>> GetHistoriques(ISender sender)
    {
        return await sender.Send(new GetHistoriquesQuery());
    }
}

