using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierDetails;
using NejPortalBackend.Application.Dossiers.Queries.GetDossierFilters;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.GetDossiers;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetDossiersQuery : IRequest<PaginatedList<DossierDto>>
{
    public IList<int>? EtatPayment { get; init; } = null;
    public IList<string>? Clients { get; init; } = null;
    public IList<string>? Agents { get; init; } = null;
    public string? CodeDossier { get; init; } = null;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}


public class GetDossiersQueryValidator : AbstractValidator<GetDossiersQuery>
{
    public GetDossiersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

public class GetDossiersQueryHandler : IRequestHandler<GetDossiersQuery, PaginatedList<DossierDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUserService;

    private readonly ILogger<GetDossiersQueryHandler> _logger;


    public GetDossiersQueryHandler(IApplicationDbContext context,   IUser currentUserService, ILogger<GetDossiersQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaginatedList<DossierDto>> Handle(GetDossiersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetDossiersQuery with parameters: PageNumber={PageNumber}, PageSize={PageSize}, CodeDossier={CodeDossier}, Clients={Clients}, Agents={Agents}, EtatPayment={EtatPayment}",
            request.PageNumber, request.PageSize, request.CodeDossier, request.Clients, request.Agents, request.EtatPayment);

        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            {
                _logger.LogWarning("Unauthorized access attempt detected.");
                throw new UnauthorizedAccessException("User is not authorized.");
            }

            var operationsQuery = _context.Operations
                                          .Where(op => !string.IsNullOrWhiteSpace(op.CodeDossier))
                                          .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.CodeDossier))
            {
                operationsQuery = operationsQuery.Where(o => !string.IsNullOrWhiteSpace(o.CodeDossier) && o.CodeDossier.Contains(request.CodeDossier));
                _logger.LogDebug("Filtering operations by CodeDossier: {CodeDossier}", request.CodeDossier);
            }

            if (request.Clients?.Count > 0)
            {
                operationsQuery = operationsQuery.Where(o => request.Clients.Contains(o.UserId));
                _logger.LogDebug("Filtering operations by Clients: {Clients}", request.Clients);
            }

            if (request.Agents?.Count > 0)
            {
                operationsQuery = operationsQuery.Where(o => !string.IsNullOrWhiteSpace(o.ReserverPar) && request.Agents.Contains(o.ReserverPar));
                _logger.LogDebug("Filtering operations by Agents: {Agents}", request.Agents);
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

            _logger.LogInformation("Successfully handled GetDossiersQuery.");
            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in GetDossiersQuery.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling GetDossiersQuery.");
            throw;
        }
    }

}
