﻿
using NejPortalBackend.Application.Common.Features.DownloadDocumentById;


namespace NejPortalBackend.Web.Endpoints;

public class ClientDocuments : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(ClientDownloadById, "download/{id}");
    }
    public async Task<IResult> ClientDownloadById(ISender sender, int id)
    {
        try
        {
            var vm = await sender.Send(new DownloadDocumentByIdQuery { DocumentId = id });

            return vm == null || vm.FileContent == null || vm.ContentType == null
                ? Results.BadRequest("Error reading file or file is empty.")
                : Results.File(vm.FileContent, vm.ContentType, vm.FileName);
        }
        catch (Exception ex)
        {
            // Log the error and return a generic error response
            // Log the error using your preferred logging method
            Console.Error.WriteLine($"Error downloading the file: {ex.Message}");

            // Return a server error response
            return Results.Problem(ex.Message, "An error occurred while processing your request.");
        }
    }
}