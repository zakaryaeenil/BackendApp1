using System.Globalization;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;
using static NejPortalBackend.Application.Common.Models.DashboardHelpers;

namespace NejPortalBackend.Application.Dashboard.Queries.GetDashboard;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetDashboardQuery : IRequest<DashboardVm>
{
    public int? Year { get; init; }
    public int? Month { get; init; }
}


public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;
    private readonly IFileService _fileService;
    private readonly ILogger<GetDashboardQueryHandler> _logger;

    public GetDashboardQueryHandler(IApplicationDbContext context, IUser currentUserService, IIdentityService identityService, IFileService fileService, ILogger<GetDashboardQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<DashboardVm> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
            throw new UnauthorizedAccessException();

        var userId = _currentUserService.Id;

        bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
        bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);
        int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

        IQueryable<Operation> operationsQuery = _context.Operations.AsNoTracking(); ;
        // Filter operations by user and criteria
        if (isAdmin)
        {
            operationsQuery = typeOperation != null ? operationsQuery.Where(o => (int)o.TypeOperation == typeOperation) : operationsQuery;
        }
        if (isAgent)
        {
            operationsQuery = typeOperation != null ? operationsQuery.Where(o => (int)o.TypeOperation == typeOperation) : throw new UnauthorizedAccessException("User is not authorized.");
        }
        if (request.Year.HasValue || request.Month.HasValue)
        {
            operationsQuery = operationsQuery
                .Where(o =>
                    (!request.Year.HasValue || o.Created.Year == request.Year.Value) &&
                    (!request.Month.HasValue || o.Created.Month == request.Month.Value)
                );
        }

        var adminDashVm = new DashboardVm();

        adminDashVm = isAdmin
            ? await GetDashboardAdminData(request, operationsQuery, adminDashVm, cancellationToken)
            : isAgent
            ? await GetDashboardAgentData(request, userId, operationsQuery, adminDashVm, cancellationToken)
            : throw new UnauthorizedAccessException();

        return adminDashVm;
    }

    private static async Task<DashboardVm> GetDashboardAgentData(GetDashboardQuery request, string userId, IQueryable<Domain.Entities.Operation> operationsQuery, DashboardVm adminDashVm, CancellationToken cancellationToken)
    {
        var agentOperationsQuery = operationsQuery.Where(o => o.ReserverPar == userId);

        adminDashVm.NbrTotalOperations = await agentOperationsQuery.CountAsync(cancellationToken);

        adminDashVm.NbrNotReservedOperations = await operationsQuery
            .Where(o => !o.EstReserver)
            .CountAsync(cancellationToken);

        adminDashVm.NbrEncoursOperations = await agentOperationsQuery
            .Where(o => o.EtatOperation != EtatOperation.cloture)
            .CountAsync(cancellationToken);

        adminDashVm.NbrTotalExportOperations = await agentOperationsQuery
            .Where(o => o.TypeOperation == TypeOperation.Export)
            .CountAsync(cancellationToken);

        adminDashVm.NbrTotalImportOperations = await agentOperationsQuery
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
            adminDashVm.OperationEtatDtos.Add(new OperationEtatDto
            {
                Etat = etat.Name,
                NumberOfOperations = count
            });
        }

        return adminDashVm;
    }
    private async Task<DashboardVm> GetDashboardAdminData(GetDashboardQuery request, IQueryable<Domain.Entities.Operation> operationsQuery, DashboardVm adminDashVm, CancellationToken cancellationToken)
    {
        var factureQuery = _context.Factures.AsQueryable();
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
            var operationsByMonth = await _context.Operations
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

            adminDashVm.ChartOperations = chartOperationByYearList;
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


        adminDashVm.NbrTotalFactures = await factureQuery.CountAsync(cancellationToken);
        adminDashVm.NbrTotalOperations = await operationsQuery.CountAsync(cancellationToken);

        adminDashVm.NbrNotReservedOperations = await operationsQuery
            .Where(o => !o.EstReserver && string.IsNullOrWhiteSpace(o.ReserverPar))
            .CountAsync(cancellationToken);

        adminDashVm.NbrEncoursOperations = await operationsQuery
            .Where(o => o.EtatOperation != EtatOperation.cloture)
            .CountAsync(cancellationToken);

        adminDashVm.NbrTotalAgents = await _identityService.GetUserInRoleCount(Roles.Agent);

        adminDashVm.NbrTotalClients = await _identityService.GetUserInRoleCount(Roles.Client);


        adminDashVm.NbrTotalExportOperations = await operationsQuery
            .Where(o => o.TypeOperation == TypeOperation.Export)
            .CountAsync(cancellationToken);

        adminDashVm.NbrTotalImportOperations = await operationsQuery
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
            adminDashVm.OperationEtatDtos.Add(new OperationEtatDto
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
            adminDashVm.FactureEtatDtos.Add(new FactureEtatDto
            {
                Etat = etatPayement.Name,
                NumberOfFactures = count
            });
        }
        //end pour Factures


        return adminDashVm;
    }
}
