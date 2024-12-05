using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Comptes.Commands.UpdateCompte;

[Authorize(Roles = Roles.Administrator)]
public record UpdateCompteCommand : IRequest<(Result Result, string? UserId)>
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string EmailNotif { get; init; }
    public required bool HasAccess { get; init; } = true;
    public string? PhoneNumber { get; init; }
    public string? CodeUser { get; init; }
}

public class UpdateCompteCommandValidator : AbstractValidator<UpdateCompteCommand>
{
    public UpdateCompteCommandValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.EmailNotif).NotNull().NotEmpty().EmailAddress().WithMessage("A valid notification email is required.");
    }
}

public class UpdateCompteCommandHandler : IRequestHandler<UpdateCompteCommand, (Result Result, string? UserId)>
{
    private readonly IIdentityService _identityService;

    public UpdateCompteCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<(Result Result, string? UserId)> Handle(UpdateCompteCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.UpdateUserAsync(request.Id, request.UserName, request.Email,request.EmailNotif, request.PhoneNumber, request.CodeUser, request.HasAccess);

    }
}
