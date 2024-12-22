using System;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Operations.Commands.ClientCreateOperation;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationCommentaires;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationDetails;
using NejPortalBackend.Application.Operations.Commands.ClientUpdateOperationDocuments;
using NejPortalBackend.Application.Operations.Commands.CreateOperation;
using NejPortalBackend.Application.Operations.Commands.ReserveOperation;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationCommentaires;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationDetails;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationDocuments;
using NejPortalBackend.Application.Operations.Queries.ClientGetAllOperations;
using NejPortalBackend.Application.Operations.Queries.ClientGetOperationDetails;
using NejPortalBackend.Application.Operations.Queries.ClientGetOperationFilters;
using NejPortalBackend.Application.Operations.Queries.GetAllOperations;
using NejPortalBackend.Application.Operations.Queries.GetExportCsvOperations;
using NejPortalBackend.Application.Operations.Queries.GetMyOperations;
using NejPortalBackend.Application.Operations.Queries.GetNotReservedOperations;
using NejPortalBackend.Application.Operations.Queries.GetOperationDetails;
using NejPortalBackend.Application.Operations.Queries.GetOperationFilters;
using NejPortalBackend.Domain.Enums;

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
        //.MapPut(ClientReserveOperation, "reserve/{id}")
        .MapPut(ClientUpdateInfosGenerale, "update-info-general/{id}")
        .MapPut(ClientUpdateDocuments, "update-documents/{id}")
        .MapPut(ClientUpdateComments, "update-commentaires/{id}")
       // .MapPost(GetClientExportCsvOperations, "export")
         ;

    }
    private async Task<OperationFiltersVm> GetClientAllFilters(ISender sender)
    {
        return await sender.Send(new ClientGetOperationFiltersQuery());
    }

    private async Task<PaginatedList<ClientOperationDto>> GetClientOperationsWithPagination(ISender sender, ClientGetAllOperationsQuery query)
    {
        return await sender.Send(query);
    }

    private async Task<IResult> CreateClientOperation(ISender sender, HttpRequest request)
    {
        try
        {
            var form = await request.ReadFormAsync();

            // Validate required form fields
            if (!int.TryParse(form["typeOperation"], out var typeOperation))
            {
                return Results.BadRequest(new { Message = "Invalid typeOperation" });
            }
            // Validate required form fields
            if (!int.TryParse(form["operationPrioriteId"], out var operationPrioriteId))
            {
                return Results.BadRequest(new { Message = "Invalid operationPrioriteId" });
            }



            if (!bool.TryParse(form["tR"], out var TR))
            {
                return Results.BadRequest(new { Message = "Invalid TR" });
            }
            if (!bool.TryParse(form["dEBOURS"], out var DEBOURS))
            {
                return Results.BadRequest(new { Message = "Invalid DEBOURS" });
            }
            if (!bool.TryParse(form["cONFIRMATION_DEDOUANEMENT"], out var CONFIRMATION_DEDOUANEMENT))
            {
                return Results.BadRequest(new { Message = "Invalid CONFIRMATION_DEDOUANEMENT" });
            }

            var commentaire = form["commentaire"].ToString();


            // Get uploaded files and ensure there is at least one file
            var files = form.Files;
            
            // Create the command for the client operation
            var command = new ClientCreateOperationCommand
            {
                TypeOperationId = typeOperation,
                OperationPrioriteId = operationPrioriteId,
                TR = TR,
                DEBOURS = DEBOURS,
                CONFIRMATION_DEDOUANEMENT = CONFIRMATION_DEDOUANEMENT,
                Commentaire = commentaire,
                Files = files
            };

            // Send command to the handler and return the result
            var operationId = await sender.Send(command);
            return Results.Ok(operationId); // Return success response with operation ID
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }



    private async Task<OperationDetailVm> GetClientOperationDetails(ISender sender, int id)
    {
        return await sender.Send(new ClientGetOperationDetailsQuery { OperationId = id });
    }

    private async Task<IResult> ClientUpdateInfosGenerale(ISender sender, int id, ClientUpdateOperationDetailsCommand command)
    {
        try
        {
            if (id != command.OperationId)
                return Results.BadRequest(new { Message = "The provided ID does not match the command's OperationId." });

            await sender.Send(command);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }

    private async Task<IResult> ClientUpdateDocuments(ISender sender, int id, HttpRequest request)
    {
        try
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
                return Results.BadRequest(new { Message = "The provided ID does not match the command's OperationId." });
            }

            await sender.Send(command);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }

    private async Task<IResult> ClientUpdateComments(ISender sender, int id, ClientUpdateOperationCommentairesCommand command)
    {
        try
        {
            if (id != command.OperationId)
                return Results.BadRequest(new { Message = "The provided ID does not match the command's OperationId." });

            await sender.Send(command);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }




    private async Task<IResult> GetClientExportCsvOperations(ISender sender, GetExportCsvOperationsQuery query)
    {
        try
        {
            var vm = await sender.Send(query);
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

