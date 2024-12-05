using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Comptes.Queries.GetComptes;


[Authorize(Roles = Roles.Administrator)]
public record GetComptesQuery : IRequest<IEnumerable<UserDto>>;



public class GetComptesQueryHandler : IRequestHandler<GetComptesQuery, IEnumerable<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetComptesQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetComptesQuery request, CancellationToken cancellationToken)
    {
            var clients = await _identityService.GetAllUsersInRoleAsync(Roles.Client);
            var agents = await _identityService.GetAllUsersInRoleAsync(Roles.Agent);

        return clients.Concat(agents);
    }
}
