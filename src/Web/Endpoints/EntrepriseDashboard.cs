using System;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Dashboard.Queries.GetDashboard;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetEntrepriseDashBoard);
    }
    public async Task<DashboardVm> GetEntrepriseDashBoard(ISender sender, [AsParameters] GetDashboardQuery query)
    {
        return await sender.Send(query);
    }
}