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
    private readonly IUser _currentUserService;
    public GetComptesQueryHandler(IIdentityService identityService, IUser currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetComptesQuery request, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {

            throw new UnauthorizedAccessException("User is not authorized.");
        }
        var clients = await _identityService.GetAllUsersInRoleAsync(Roles.Client);
        var agents = await _identityService.GetAllUsersInRoleAsync(Roles.Agent);

        bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
        bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

        int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

        
        if (isAdmin)
        {
            agents = typeOperation != null ? agents.Where(o => o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : agents;
        }
        if (isAgent)
        {
            agents = typeOperation != null ? agents.Where(o => o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : throw new UnauthorizedAccessException("User is not authorized.");
        }
        return clients.Concat(agents);
    }
}
