using System;
using MediatR;
using System.Threading;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Comptes.Commands.CreateCompte;
using NejPortalBackend.Application.Comptes.Queries.GetComptes;
using NejPortalBackend.Application.Operations.Commands.CreateOperation;
using NejPortalBackend.Application.Comptes.Commands.UpdateCompte;
using NejPortalBackend.Application.Comptes.Commands.ChangePasswordCompte;
using Microsoft.AspNetCore.Components.Forms;
using NejPortalBackend.Application.Comptes.Queries.GetClientsNotHaveCompte;

namespace NejPortalBackend.Web.Endpoints;

public class EntrepriseComptes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(EntrepriseGetClientDontHaveComptes, "filters")
            .MapGet(EntrepriseGetAllComptes, "all")
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
                return Results.BadRequest(result.Errors);
            }

            return Results.Ok(new { UserId = userId });
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }

    private async Task<IResult> EntrepriseUpdateCompte(ISender sender, UpdateCompteCommand command)
    {
        try
        {
            var (result, userId) = await sender.Send(command);

            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors);
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