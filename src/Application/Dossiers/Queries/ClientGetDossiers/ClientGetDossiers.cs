using System.Data;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Dossiers.Queries.GetDossiers;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.ClientGetDossiers;

[Authorize(Roles = Roles.Client)]
public record ClientGetDossiersQuery : IRequest<PaginatedList<DossierDto>>
{
    public IList<int>? EtatPayment { get; init; } = null;
    public string? CodeDossier { get; init; } = null;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class ClientGetDossiersQueryValidator : AbstractValidator<ClientGetDossiersQuery>
{
    public ClientGetDossiersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
          .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");

    }
}

public class ClientGetDossiersQueryHandler : IRequestHandler<ClientGetDossiersQuery, PaginatedList<DossierDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<ClientGetDossiersQueryHandler> _logger;

    public ClientGetDossiersQueryHandler(IIdentityService identityService, IApplicationDbContext context, IUser currentUserService, ILogger<ClientGetDossiersQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _identityService = identityService;
    }

    public async Task<PaginatedList<DossierDto>> Handle(ClientGetDossiersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetDossiersQueryHandler received with parameters: {Request}", request);

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
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            {
                _logger.LogWarning("Unauthorized access attempt detected.");
                throw new UnauthorizedAccessException("User is not authorized.");
            }

            var operationsQuery = _context.Operations
                                          .Where(op => !string.IsNullOrWhiteSpace(op.CodeDossier) && op.UserId  == _currentUserService.Id)
                                          .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.CodeDossier))
            {
                operationsQuery = operationsQuery.Where(o => !string.IsNullOrWhiteSpace(o.CodeDossier) && o.CodeDossier.Contains(request.CodeDossier));
                _logger.LogDebug("Filtering operations by CodeDossier: {CodeDossier}", request.CodeDossier);
            }

            var dossiersListQuery = operationsQuery.GroupBy(f => f.CodeDossier)
                                                   .Select(g => new DossierDto
                                                   {
                                                       CodeDossier = g.Key,
                                                       NombreOperations = g.Count(),
                                                       NombreFactures = _context.Factures.Count(f => f.CodeDossier == g.Key),
                                                       Desription = string.Join("\n", _context.Factures.Where(f => f.CodeDossier == g.Key).Select(f => f.Description)),
                                                       MontantTotal = _context.Factures.Where(f => f.CodeDossier == g.Key).Sum(f => f.MontantTotal),
                                                       MontantPaye = _context.Factures.Where(f => f.CodeDossier == g.Key).Sum(f => f.MontantPaye),
                                                       MontantReste = _context.Factures.Where(f => f.CodeDossier == g.Key).Sum(f => f.MontantTotal - f.MontantPaye),
                                                       EtatPayement = _context.Factures.Any(f => f.CodeDossier == g.Key && f.EtatPayement == EtatPayement.PayementIncomplet) ?
                                                                      EtatPayement.PayementIncomplet : _context.Factures.All(f => f.CodeDossier == g.Key && f.EtatPayement == EtatPayement.Payée) ?
                                                                      EtatPayement.Payée : EtatPayement.Impayée
                                                   })
                                                   .AsNoTracking();

            if (request.EtatPayment?.Count > 0)
            {
                dossiersListQuery = dossiersListQuery.Where(d => request.EtatPayment.Contains((int)d.EtatPayement));
                _logger.LogDebug("Filtering dossiers by EtatPayment: {EtatPayment}", request.EtatPayment);
            }

            _logger.LogInformation("Executing paginated query for Dossiers.");
            var result = await dossiersListQuery.PaginatedListAsync(request.PageNumber, request.PageSize);

            _logger.LogInformation("Successfully handled ClientGetDossiersQueryHandler.");
            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in ClientGetDossiersQueryHandler.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling ClientGetDossiersQueryHandler.");
            throw;
        }
    }
}
