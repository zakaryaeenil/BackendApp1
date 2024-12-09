using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Historiques.Queries.ClientGetHistoriques;

namespace NejPortalBackend.Web.Endpoints;

public class ClientHistoriques : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(ClientGetHistoriques, "all");
    }
    public async Task<IList<HistoriqueDto>> ClientGetHistoriques(ISender sender)
    {
        return await sender.Send(new ClientGetHistoriquesQuery());
    }
}

