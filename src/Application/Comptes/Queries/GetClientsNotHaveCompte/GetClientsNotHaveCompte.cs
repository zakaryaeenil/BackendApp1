using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Comptes.Queries.GetClientsNotHaveCompte;

public record GetClientsNotHaveCompteQuery : IRequest<IList<ClientDto>>;



public class GetClientsNotHaveCompteQueryHandler : IRequestHandler<GetClientsNotHaveCompteQuery, IList<ClientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IMapper _mapper;
    public GetClientsNotHaveCompteQueryHandler(IMapper mapper,IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
        _mapper = mapper;
    }

    public async Task<IList<ClientDto>> Handle(GetClientsNotHaveCompteQuery request, CancellationToken cancellationToken)
    {
        // Step 1: Get the list of user `CodeRef` values in memory
        var users = await _identityService.GetAllUsersInRoleAsync(Roles.Client);
        var userCodeRefs = users
            .Where(u => !string.IsNullOrWhiteSpace(u.CodeRef))
            .Select(u => u.CodeRef)
            .ToHashSet(); // Use a HashSet for efficient lookups

        // Step 2: Filter clients not in the userCodeRefs
        var result = _context.Clients
            .Where(c => !userCodeRefs.Contains(c.CodeClient));

        // Step 3: Map and return the result
        return await result
            .ProjectTo<ClientDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

}
