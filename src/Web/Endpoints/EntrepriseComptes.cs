using System;
using MediatR;
using System.Threading;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Comptes.Commands.CreateCompte;
using NejPortalBackend.Application.Comptes.Queries.GetComptes;
using NejPortalBackend.Application.Operations.Commands.CreateOperation;
using Microsoft.AspNetCore.Components.Forms;
using NejPortalBackend.Application.Comptes.Queries.GetClientsNotHaveCompte;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Operations.Queries.GetOperationDetails;
using NejPortalBackend.Application.Comptes.Queries.GetCompteDetails;
using NejPortalBackend.Application.Comptes.Queries.GetCompte;
using NejPortalBackend.Application.Comptes.Commands.UpdateCompte;
using NejPortalBackend.Application.Common.Features.ChangePasswordCompte;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseComptes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(EntrepriseGetClientDontHaveComptes, "filters")
            .MapGet(GetCompteById, "{id}")
            .MapGet(EntrepriseGetAllComptes, "all")
            .MapGet(GetEntrepriseCompteDetails, "details")
            .MapPost(EntrepriseCreateCompte, "create")
            .MapPost(EntrepriseUpdatePasswordCompte, "update-password")
            .MapPost(EntrepriseUpdateCompte, "update/{id}");
    }
    private async Task<IList<ClientDto>> EntrepriseGetClientDontHaveComptes(ISender sender)
    {
        return await sender.Send(new GetClientsNotHaveCompteQuery());
    }
    // Get all agents
    private async Task<IEnumerable<UserDto>> EntrepriseGetAllComptes(ISender sender)
    {
        return await sender.Send(new GetComptesQuery());
    }
    private async Task<IResult> EntrepriseCreateCompte(ISender sender, CreateCompteCommand command)
    {
        try
        {
            var (result, userId) = await sender.Send(command);

            if (!result.Succeeded)
            {
                          // Return structured error information
                return Results.BadRequest(new { Message = "Failed to create user.", Errors = result.Errors?? Array.Empty<string>() });
            }

            return Results.Ok(new { UserId = userId });
        }
        catch (Exception ex)
        {

            // Return a detailed error message for debugging
            return Results.Problem(
                detail: ex.Message,
                title: "An error occurred while creating the user",
                statusCode: 500
            );
        }
    }

    private async Task<CompteDetailsVm> GetEntrepriseCompteDetails(ISender sender, [AsParameters] GetCompteDetailsQuery query)
    {
        return await sender.Send(query);
    }
    private async Task<UserDto> GetCompteById(ISender sender, string id)
    {
            return await sender.Send(new GetCompteQuery { Id = id });
    }

    private async Task<IResult> EntrepriseUpdateCompte(ISender sender, string id, UpdateCompteCommand command)
    {
        try
        {
            if (id != command.Id)
                return Results.BadRequest(new { Message = "The provided ID does not match the command's OperationId." });

            var (result, userId) = await sender.Send(command);

            if (!result.Succeeded)
            {
                return Results.BadRequest(new { Message = "Failed to update user.", Errors = result.Errors ?? Array.Empty<string>() });
            }

            return Results.Ok(new { UserId = userId });
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }
    private async Task<IResult> EntrepriseUpdatePasswordCompte(ISender sender, ChangePasswordCompteCommand command)
    {
        try
        {
            // Validate and execute the command using MediatR
            var (result, userId) = await sender.Send(command);

            return result.Succeeded
                ? Results.Ok(new { Message = "Password changed successfully." })
                : Results.BadRequest(new { Errors = result.Errors });
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }

}