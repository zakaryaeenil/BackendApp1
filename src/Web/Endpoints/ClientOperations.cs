using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Operations.Commands.ClientCreateOperation;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationCommentaires;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationDetails;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationDocuments;
using NejPortalBackend.Application.Operations.Queries.ClientGetAllOperations;
using NejPortalBackend.Application.Operations.Queries.ClientGetOperationDetails;
using NejPortalBackend.Application.Operations.Queries.ClientGetOperationFilters;

namespace NejPortalBackend.Web.Endpoints;

public class ClientOperations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
         .RequireAuthorization()
         .MapGet(GetClientAllFilters, "filters")
         .MapGet(GetClientOperationDetails, "details/{id}")
         .MapPost(GetClientOperationsWithPagination, "all")
         .MapPost(CreateClientOperation, "create")
         //.MapPost(GetClientOperationsExport, "export")
         .MapPut(ClientUpdateInfosGenerale, "update-info-general/{id}")
         .MapPut(ClientUpdateDocuments, "update-documents/{id}")
         .MapPut(ClientUpdateComments, "update-commentaires/{id}");
    }
    private async Task<OperationFiltersVm> GetClientAllFilters(ISender sender)
    {
        return await sender.Send(new ClientGetOperationFiltersQuery());
    }

    private async Task<PaginatedList<ClientOperationDto>> GetClientOperationsWithPagination(ISender sender, ClientGetAllOperationsQuery query)
    {
        return await sender.Send(query);
    }

    //private async Task<ExportOperationsVM> GetClientOperationsExport(ISender sender, GetClientExportOperationsQuery query)
    //{
    //    return await sender.Send(query);

    //}
    private async Task<int> CreateClientOperation(ISender sender, HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        // Get form data
        var typeOperation = int.Parse(form["typeOperation"].ToString());
        var commentaire = form["commentaire"].ToString();
        // Get uploaded files
        var files = form.Files;

        // Create the command for the client operation
        var command = new ClientCreateOperationCommand
        {
            TypeOperationId = typeOperation,
            Commentaire = commentaire,
            Files = files
        };
        return await sender.Send(command);
    }

    private async Task<OperationDetailVm> GetClientOperationDetails(ISender sender, int id)
    {
        return await sender.Send(new ClientGetOperationDetailsQuery { OperationId = id });
    }

    private async Task<IResult> ClientUpdateInfosGenerale(ISender sender, int id, ClientUpdateOperationDetailsCommand command)
    {
        if (id != command.OperationId)
            return Results.BadRequest("The provided ID does not match the command's OperationId.");

        await sender.Send(command);
        return Results.NoContent();
    }

    private async Task<IResult> ClientUpdateDocuments(ISender sender, int id, HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        if (!int.TryParse(form["operationId"], out var operationId))
        {
            return Results.BadRequest("Invalid operation ID.");
        }
        var newFiles = form.Files;

        var command = new ClientUpdateOperationDocumentsCommand
        {
            OperationId = operationId,
            Files = newFiles.Any() ? newFiles : null, // Only pass files if they exist
        };

        if (id != command.OperationId)
        {
            return Results.BadRequest("The provided ID does not match the command's OperationId.");
        }

        await sender.Send(command);

        return Results.NoContent();
    }

    private async Task<IResult> ClientUpdateComments(ISender sender, int id, ClientUpdateOperationCommentairesCommand command)
    {
        if (id != command.OperationId)
            return Results.BadRequest("The provided ID does not match the command's OperationId.");

        await sender.Send(command);
        return Results.NoContent();
    }


}

