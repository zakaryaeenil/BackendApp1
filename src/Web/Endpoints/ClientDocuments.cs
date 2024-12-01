using System;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Features.DownloadDocumentById;

namespace NejPortalBackend.Web.Endpoints;

public class ClientDocuments : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(ClientDownloadById, "download/{id}");
    }
    public async Task<DownloadDocumentVm> ClientDownloadById(ISender sender, int id)
    {
        var vm = await sender.Send(new DownloadDocumentByIdQuery { DocumentId = id });

        return vm == null || vm.FileContent == null || vm.ContentType == null
            ? throw new Exception("Error reading file")
            : vm;
    }
}