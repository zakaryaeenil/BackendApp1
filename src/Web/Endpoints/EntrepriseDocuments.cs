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
    public async Task<IResult> EntrepriseDownloadById(ISender sender, int id)
    {
        try
        {
            var vm = await sender.Send(new DownloadDocumentByIdQuery { DocumentId = id });

            if (vm == null || vm.FileContent == null || vm.ContentType == null)
            {
                return Results.BadRequest("Error reading file or file is empty.");
            }

            // Create a response object containing the file content and filename
            var fileResponse = new DownloadDocumentVm
            {
                FileContent = vm.FileContent,
                FileName = vm.FileName,
                ContentType = vm.ContentType
            };

            return Results.Ok(fileResponse); // Send as a JSON object
        }
        catch (Exception ex)
        {
            // Log the error and return a generic error response
            // Log the error using your preferred logging method
            Console.Error.WriteLine($"Error downloading the file: {ex.Message}");

            // Return a server error response
            return Results.Problem("An error occurred while processing your request.");
        }
    }

}