using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Comptes.Commands.CreateCompte;

[Authorize(Roles = Roles.Administrator)]
public record CreateCompteCommand : IRequest<(Result Result, string? UserId)>
{
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string Email_Notif { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Password { get; init; }
    public string? CodeUser { get; init; }
}

public class CreateCompteCommandValidator : AbstractValidator<CreateCompteCommand>
{
    public CreateCompteCommandValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Email_Notif).NotNull().NotEmpty().EmailAddress().WithMessage("A valid notification email is required.");
        RuleFor(x => x.Password).NotNull().NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateCompteCommandHandler : IRequestHandler<CreateCompteCommand, (Result Result, string? UserId)>
{
    private readonly IIdentityService _identityService;


    public CreateCompteCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<(Result Result, string? UserId)> Handle(CreateCompteCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.CreateUserAsync(request.UserName, request.Password, request.Email, request.PhoneNumber, request.CodeUser,request.Email_Notif, cancellationToken);
    }
}
