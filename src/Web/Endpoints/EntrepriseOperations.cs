﻿using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Operations.Commands.CreateOperation;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationCommentaires;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationDetails;
using NejPortalBackend.Application.Operations.Commands.UpdateOperationDocuments;
using NejPortalBackend.Application.Operations.Queries.GetAllOperations;
using NejPortalBackend.Application.Operations.Queries.GetMyOperations;
using NejPortalBackend.Application.Operations.Queries.GetNotReservedOperations;
using NejPortalBackend.Application.Operations.Queries.GetOperationDetails;
using NejPortalBackend.Application.Operations.Queries.GetOperationFilters;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseOperations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetEntrepriseAllFilters, "filters")
        .MapGet(GetEntrepriseOperationDetails, "details/{id}")
        .MapPost(GetEntrepriseOperationsWithPagination, "all")
        .MapPost(GetEntrepriseMyOperationsWithPagination, "my")
        .MapPost(GetEntrepriseNotResOperationsWithPagination, "not-reserved")
        .MapPost(CreateEntrepriseOperation, "create")
        //.MapPut(EntrepriseReserveOperation, "reserve/{id}")
        .MapPut(EntrepriseUpdateInfosGenerale, "update-info-general/{id}")
        .MapPut(EntrepriseUpdateDocuments, "update-documents/{id}")
        .MapPut(EntrepriseUpdateComments, "update-commentaires/{id}");

    }
    private async Task<OperationFiltersVm> GetEntrepriseAllFilters(ISender sender)
    {
        return await sender.Send(new GetOperationFiltersQuery());
    }

    private async Task<PaginatedList<OperationDto>> GetEntrepriseOperationsWithPagination(ISender sender, GetAllOperationsQuery query)
    {
        return await sender.Send(query);
    }

    private async Task<PaginatedList<OperationDto>> GetEntrepriseMyOperationsWithPagination(ISender sender, GetMyOperationsQuery query)
    {
        return await sender.Send(query);
    }

    private async Task<PaginatedList<OperationDto>> GetEntrepriseNotResOperationsWithPagination(ISender sender, GetNotReservedOperationsQuery query)
    {
        return await sender.Send(query);
    }

    //private async Task<IResult> EntrepriseReserveOperation(ISender sender, int id, ReserveOperationCommand command)
    //{
    //  if (id != command.Id) return Results.BadRequest();
    //await sender.Send(command);
    // return Results.NoContent();
    //}

    private async Task<IResult> CreateEntrepriseOperation(ISender sender,HttpRequest request)
    {
        try
        {
            var form = await request.ReadFormAsync();

            // Validate required form fields
            if (!int.TryParse(form["typeOperation"], out var typeOperation))
            {
                return Results.BadRequest("Invalid typeOperation.");
            }

            var commentaire = form["commentaire"].ToString();

            var client = form["clientId"].ToString();
            if (string.IsNullOrWhiteSpace(client))
            {
                return Results.BadRequest("Client ID is required.");
            }

            var agent = form["agentId"].ToString();
            

            // Get uploaded files and ensure there is at least one file
            var files = form.Files;
            

            // Create the command for the client operation
            var command = new CreateOperationCommand
            {
                ClientId = client,
                AgentId = agent,
                TypeOperationId = typeOperation,
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



    private async Task<OperationDetailVm> GetEntrepriseOperationDetails(ISender sender, int id)
    {
        return await sender.Send(new GetOperationDetailsQuery { OperationId = id });
    }

    private async Task<IResult> EntrepriseUpdateInfosGenerale(ISender sender, int id, UpdateOperationDetailsCommand command)
    {
        if (id != command.OperationId)
            return Results.BadRequest("The provided ID does not match the command's OperationId.");

        await sender.Send(command);
        return Results.NoContent();
    }

    private async Task<IResult> EntrepriseUpdateDocuments(ISender sender, int id, HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        if (!int.TryParse(form["operationId"], out var operationId))
        {
            return Results.BadRequest("Invalid operation ID.");
        }

        // Parse the list of documentIds
        var documentIdsString = form["documentIds"];
        List<int> documentIds = new List<int>();

        if (!string.IsNullOrEmpty(documentIdsString))
        {
            var documentIdsArray = documentIdsString.ToString().Split(',');

            foreach (var docId in documentIdsArray)
            {
                if (int.TryParse(docId.Trim(), out var parsedId))
                {
                    documentIds.Add(parsedId);
                }
                else
                {
                    return Results.BadRequest($"Invalid document ID: {docId}");
                }
            }
        }
        var newFiles = form.Files;

        var command = new UpdateOperationDocumentsCommand
        {
            OperationId = operationId,
            DocumentIds = documentIds,
            Files = newFiles.Any() ? newFiles : null, // Only pass files if they exist
        };

        if (id != command.OperationId)
        {
            return Results.BadRequest("The provided ID does not match the command's OperationId.");
        }

        await sender.Send(command);

        return Results.NoContent();
    }

    private async Task<IResult> EntrepriseUpdateComments(ISender sender, int id, UpdateOperationCommentairesCommand command)
    {
        if (id != command.OperationId)
            return Results.BadRequest("The provided ID does not match the command's OperationId.");

        await sender.Send(command);
        return Results.NoContent();
    }
}