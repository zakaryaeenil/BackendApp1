using System;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Comptes.Commands.CreateCompte;
using NejPortalBackend.Application.Features.Auth;

namespace NejPortalBackend.Web.Endpoints;

public class Authentification : EndpointGroupBase
{
            public override void Map(WebApplication app)
        {
            app.MapGroup(this)
                .AllowAnonymous()
                .MapPost(PostLogin, "login")
                 .MapPost(PostResetPassword, "reset-password")
                .MapPost(PostRefreshToken, "refresh-token");

        }
        private async Task<LoginResponse> PostLogin(ISender sender, AuthenticateCommand command)
        {
            return string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password) || string.IsNullOrWhiteSpace(command.AppIdentifier)
                ? new LoginResponse
                {
                    Error = "Invalid login credentials."
                }
                : await sender.Send(command);
        }
        private async Task<LoginResponse> PostRefreshToken(ISender sender, RefreshTokenCommand command)
        {
            return string.IsNullOrWhiteSpace(command.RefreshToken)
                ? new LoginResponse
                {
                    Error = "Invalid Refresh Token."
                }
                : await sender.Send(command);
        }
    private async Task<IResult> PostResetPassword(ISender sender, ResetPasswordCommand command)
    {
        try
        {
            var result = await sender.Send(command);

            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging service here)
            return Results.Problem($"An error occurred while processing your request: {ex.Message}");
        }
    }
}

