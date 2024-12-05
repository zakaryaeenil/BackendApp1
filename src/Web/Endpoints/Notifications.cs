using System;
using Microsoft.AspNetCore.Builder;
using NejPortalBackend.Infrastructure.Identity;
using NejPortalBackend.Web.Hubs;

namespace NejPortalBackend.Web.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Directly map the SignalR Hub route
        app.MapHub<NotificationHub>("/notificationHub")
           .RequireAuthorization(); // Ensure authorization if needed
    }
}

