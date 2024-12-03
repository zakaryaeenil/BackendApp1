using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierDetails;
using NejPortalBackend.Application.Dossiers.Queries.GetDossiers;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Historiques.Queries.GetHistoriques;


[Authorize(Roles = Roles.AdminAndAgent)]
public record GetHistoriquesQuery : IRequest<IList<HistoriqueDto>>;

public class GetHistoriquesQueryHandler : IRequestHandler<GetHistoriquesQuery, IList<HistoriqueDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetHistoriquesQueryHandler> _logger;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    public GetHistoriquesQueryHandler(IUser currentUserService, IIdentityService identityService,IMapper mapper,IApplicationDbContext context, ILogger<GetHistoriquesQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _mapper = mapper;
        _identityService = identityService;
    }

    public async Task<IList<HistoriqueDto>> Handle(GetHistoriquesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetHistoriquesQuery");

        try
        {
            // Check if the user is authenticated
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            {
                _logger.LogWarning("Unauthorized access attempt detected.");
                throw new UnauthorizedAccessException("User is not authorized.");
            }

            // Check user roles
            var isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
            var isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

            IQueryable<Historique> query;

            if (isAgent)
            {
                // Agent-specific filter
                query = _context.Historiques
                    .Where(h => _context.Operations
                        .Any(o => o.Id == h.OperationId &&
                                  !string.IsNullOrWhiteSpace(o.ReserverPar) &&
                                  o.ReserverPar == _currentUserService.Id))
                    .OrderByDescending(h => h.LastModified)
                    .Take(30);
            }
            else if (isAdmin)
            {
                // Admin gets all records
                query = _context.Historiques
                    .OrderByDescending(h => h.LastModified)
                    .Take(100);
            }
            else
            {
                _logger.LogWarning("Unauthorized access attempt detected.");
                throw new UnauthorizedAccessException("User is not authorized.");
            }

            // Map and return results
            return await query
                .ProjectTo<HistoriqueDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access in GetHistoriquesQuery handler.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling GetHistoriquesQuery.");
            throw;
        }
    }

}
