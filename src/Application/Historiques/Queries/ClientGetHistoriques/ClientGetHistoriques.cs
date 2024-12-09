using System.Data;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Historiques.Queries.GetHistoriques;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Historiques.Queries.ClientGetHistoriques;


[Authorize(Roles = Roles.Client)]
public record ClientGetHistoriquesQuery : IRequest<IList<HistoriqueDto>>;


public class ClientGetHistoriquesQueryHandler : IRequestHandler<ClientGetHistoriquesQuery, IList<HistoriqueDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientGetHistoriquesQueryHandler> _logger;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public ClientGetHistoriquesQueryHandler(IUser currentUserService, IIdentityService identityService, IMapper mapper, IApplicationDbContext context, ILogger<ClientGetHistoriquesQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _mapper = mapper;
        _identityService = identityService;
    }

    public async Task<IList<HistoriqueDto>> Handle(ClientGetHistoriquesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetHistoriquesQueryHandler received with parameters: {Request}", request);

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate client role
        var isClient = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Client);
        if (!isClient)
        {
            _logger.LogWarning("User {UserId} attempted to access client operations without proper role.", _currentUserService.Id);
            throw new InvalidOperationException("User " + _currentUserService.Id + " does not have the required role.");
        }

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

            IQueryable<Historique> query =_context.Historiques
                    .Where(h => _context.Operations
                        .Any(o => o.Id == h.OperationId &&
                                  o.UserId == _currentUserService.Id))
                    .OrderByDescending(h => h.LastModified)
                    .Take(30);

            // Map and return results
            return await query
                .ProjectTo<HistoriqueDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex.Message, "Unauthorized access in ClientGetHistoriquesQueryHandler handler.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while handling ClientGetHistoriquesQueryHandler.");
            throw;
        }
    }
}
