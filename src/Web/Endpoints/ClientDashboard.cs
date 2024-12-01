using System;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dashboard.Queries.ClientGetDashboard;

namespace NejPortalBackend.Web.Endpoints;

public class ClientDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetClientDashBoard);
    }
    public async Task<DashboardVm> GetClientDashBoard(ISender sender, [AsParameters] ClientGetDashboardQuery query)
    {
        return await sender.Send(query);
    }
}

