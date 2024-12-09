using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Comptes.Queries.GetCompte;

[Authorize(Roles = Roles.Administrator)]
public record GetCompteQuery : IRequest<UserDto>
{
    public required string Id { get; init; }
}

public class GetCompteQueryValidator : AbstractValidator<GetCompteQuery>
{
    public GetCompteQueryValidator()
    {
        RuleFor(v => v.Id).NotEmpty()
            .NotNull().WithMessage("Id Idantifiant required.");
    }
}

public class GetCompteQueryHandler : IRequestHandler<GetCompteQuery, UserDto>
{
    private readonly IIdentityService _identityService;

    public GetCompteQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDto> Handle(GetCompteQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByIdAsync(request.Id);
        return user != null ? user : throw new NotFoundException(nameof(UserDto), "Not Found user");
    }
}
