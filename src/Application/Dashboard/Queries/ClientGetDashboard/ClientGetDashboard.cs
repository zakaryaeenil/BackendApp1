using System.Globalization;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;
using static NejPortalBackend.Application.Common.Models.DashboardHelpers;

namespace NejPortalBackend.Application.Dashboard.Queries.ClientGetDashboard;

[Authorize(Roles = Roles.Client)]
public record ClientGetDashboardQuery : IRequest<DashboardVm>
{
    public int? Year { get; init; } 
    public int? Month { get; init; }
}

public class ClientGetDashboardQueryHandler : IRequestHandler<ClientGetDashboardQuery, DashboardVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<ClientGetDashboardQueryHandler> _logger;

    public ClientGetDashboardQueryHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<ClientGetDashboardQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<DashboardVm> Handle(ClientGetDashboardQuery request, CancellationToken cancellationToken)
    {
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
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required role.");
        }

          // Validate client role
        var codeClient = await _identityService.GetCodeClientAsync(_currentUserService.Id);
        if (string.IsNullOrWhiteSpace(codeClient))
        {
            _logger.LogWarning("User {UserId} attempted to access client operations without proper codeClient.", _currentUserService.Id);
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required codeClient.");
        }
        
        var clientDashboardVm = new DashboardVm();
        var operationsQuery = _context.Operations.Where(o => o.UserId == _currentUserService.Id).AsNoTracking();
        var factureQuery = _context.Factures.Where(f => !string.IsNullOrWhiteSpace(f.CodeClient) && f.CodeClient == codeClient).AsNoTracking();

        if (request.Year.HasValue)
        {

            // List of all months with abbreviations
            var allMonths = Enumerable.Range(1, 12)
                .Select(month => new ChartOperationByYear
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                    NumberTotalOfOperations = 0,
                    NumberImportOfOperations = 0,
                    NumberExportOfOperations = 0
                })
                .ToList();

            // Fetch operations data for the specific year and group by month
            var operationsByMonth = await operationsQuery
                .Where(o => o.Created.Year == request.Year.Value)
                .GroupBy(o => o.Created.Month)
                .Select(g => new ChartOperationByYear
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                    NumberTotalOfOperations = g.Count(),
                    NumberImportOfOperations = g.Count(o => o.TypeOperation == TypeOperation.Import),
                    NumberExportOfOperations = g.Count(o => o.TypeOperation == TypeOperation.Export)
                })
                .ToListAsync(cancellationToken);

            // Merge the allMonths list with the actual data
            var chartOperationByYearList = allMonths
                .GroupJoin(
                    operationsByMonth, // Actual data
                    allMonth => allMonth.Month, // Key from all months (abbreviation)
                    operationMonth => operationMonth.Month, // Key from operation months (abbreviation)
                    (allMonth, operationGroup) => operationGroup
                        .DefaultIfEmpty(allMonth) // If no data for this month, use allMonth with zero counts
                        .First()
                )
                .ToList();

            clientDashboardVm.ChartOperations = chartOperationByYearList;
        }

        if (request.Year.HasValue || request.Month.HasValue)
        {
            operationsQuery = operationsQuery
                .Where(o =>
                    (!request.Year.HasValue || o.Created.Year == request.Year.Value) &&
                    (!request.Month.HasValue || o.Created.Month == request.Month.Value)
                );
            factureQuery = factureQuery
              .Where(f =>
                  (!request.Year.HasValue || (f.DateEmission.HasValue && f.DateEmission.Value.Year == request.Year.Value)) &&
                  (!request.Month.HasValue || (f.DateEmission.HasValue && f.DateEmission.Value.Month == request.Month.Value))
              );
        }

        clientDashboardVm.NbrTotalFactures = await factureQuery.CountAsync(cancellationToken);
        clientDashboardVm.NbrTotalOperations = await operationsQuery.CountAsync(cancellationToken);
        clientDashboardVm.NbrTotalExportOperations = await operationsQuery
            .Where(o => o.TypeOperation == TypeOperation.Export)
            .CountAsync(cancellationToken);

        clientDashboardVm.NbrTotalImportOperations = await operationsQuery
            .Where(o => o.TypeOperation == TypeOperation.Import)
            .CountAsync(cancellationToken);


        var etatOperationsList = Enum.GetValues(typeof(EtatOperation))
            .Cast<EtatOperation>()
            .Select(p => new EtatOperationDto { Value = (int)p, Name = p.ToString() })
            .ToList();

        foreach (EtatOperationDto etat in etatOperationsList)
        {
            // Count the number of clientOperationsQuery where the type matches the current enum value
            var count = await operationsQuery
                .Where(o => (int)o.EtatOperation == etat.Value)
                .CountAsync(cancellationToken);

            // Add the result to the DTO list
            clientDashboardVm.OperationEtatDtos.Add(new OperationEtatDto
            {
                Etat = etat.Name,
                NumberOfOperations = count
            });
        }

        //start pour Factures
        var etatPayementsList = Enum.GetValues(typeof(EtatPayement))
                                      .Cast<EtatPayement>()
                                      .Select(p => new EtatPaymentDto { Value = (int)p, Name = p.ToString() })
                                      .ToList();
        foreach (EtatPaymentDto etatPayement in etatPayementsList)
        {

            var count = await factureQuery
                .Where(o => (int)o.EtatPayement == etatPayement.Value)
                .CountAsync(cancellationToken);

            // Add the result to the DTO list
            clientDashboardVm.FactureEtatDtos.Add(new FactureEtatDto
            {
                Etat = etatPayement.Name,
                NumberOfFactures = count
            });
        }
        //end pour Factures

        return clientDashboardVm;
    }
}
