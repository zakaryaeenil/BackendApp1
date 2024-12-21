using NejPortalBackend.Application.Common.Features.ChangePasswordCompte;
using NejPortalBackend.Application.Comptes.Commands.CreateCompte;
using NejPortalBackend.Infrastructure.Identity;

namespace NejPortalBackend.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(UserChangePassword, "change-password");
    }
    private async Task<IResult> UserChangePassword(ISender sender, ChangePasswordCompteCommand command)
    {
        try
        {
            var (result, userId) = await sender.Send(command);

            if (!result.Succeeded)
            {
                // Return structured error information
                return Results.BadRequest(new { Message = "Failed to create user.", Errors = result.Errors ?? Array.Empty<string>() });
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

}
