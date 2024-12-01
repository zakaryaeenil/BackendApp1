using System;
using Microsoft.AspNetCore.Mvc;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Features.DownloadDocumentById;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseDocuments : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(EntrepriseDownloadById, "download/{id}");
    }
    public async Task<ActionResult<DownloadDocumentVm>> EntrepriseDownloadById(ISender sender, int id)
    {
        var vm = await sender.Send(new DownloadDocumentByIdQuery { DocumentId = id });

        return vm == null || vm.FileContent == null || vm.ContentType == null
            ? throw new Exception("Error reading file")
            : vm;
    }
}