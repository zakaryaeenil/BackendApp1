using Microsoft.Extensions.Logging;
using System.Globalization;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;
using static NejPortalBackend.Application.Common.Models.DashboardHelpers;

namespace NejPortalBackend.Application.Comptes.Queries.GetCompteDetails;

[Authorize(Roles = Roles.Administrator)]
public record GetCompteDetailsQuery : IRequest<CompteDetailsVm>
{
    public required string Id { get; init; }
    public int? Year { get; init; } = null;
    public int? Month { get; init; } = null;
}

public class GetCompteDetailsQueryValidator : AbstractValidator<GetCompteDetailsQuery>
{
    public GetCompteDetailsQueryValidator()
    {
        RuleFor(v => v.Id).NotEmpty()
              .NotNull().WithMessage("Client Idantifiant required.");
    }

}

public class GetCompteDetailsQueryHandler : IRequestHandler<GetCompteDetailsQuery, CompteDetailsVm>
{
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompteDetailsQueryHandler> _logger;

    public GetCompteDetailsQueryHandler(
            IApplicationDbContext context,
            IIdentityService identityService,
            IMapper mapper,
            ILogger<GetCompteDetailsQueryHandler> logger)
    {
        _identityService = identityService;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CompteDetailsVm> Handle(GetCompteDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling GetUserDetailsQuery for user ID {UserId}.", request.Id);

            var user = await _identityService.GetUserByIdAsync(request.Id, cancellationToken)
                        ?? throw new NotFoundException(nameof(UserDto), request.Id);

            _logger.LogInformation("User found with ID {UserId}.", user.Id);

            var isAgent = await _identityService.IsInRoleAsync(user.Id ?? string.Empty, Roles.Agent);
            var isClient = await _identityService.IsInRoleAsync(user.Id ?? string.Empty, Roles.Client);

            _logger.LogInformation("User roles determined: IsAgent={IsAgent}, IsClient={IsClient}.", isAgent, isClient);

            var operationsQuery = isAgent
                ? _context.Operations.Where(o => o.ReserverPar == user.Id).AsNoTracking()
                : _context.Operations.Where(o => !string.IsNullOrWhiteSpace(user.CodeRef) && o.UserId == user.Id).AsNoTracking();

            var userDashVm = new CompteDetailsVm();

            if (request.Year.HasValue)
            {
                _logger.LogInformation("Fetching chart data for year {Year}.", request.Year.Value);

                var allMonths = Enumerable.Range(1, 12)
                    .Select(month => new ChartOperationByYear
                    {
                        Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                        NumberTotalOfOperations = 0,
                        NumberImportOfOperations = 0,
                        NumberExportOfOperations = 0
                    })
                    .ToList();

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

                userDashVm.ChartOperations = allMonths
                    .GroupJoin(
                        operationsByMonth,
                        allMonth => allMonth.Month,
                        operationMonth => operationMonth.Month,
                        (allMonth, operationGroup) => operationGroup
                            .DefaultIfEmpty(allMonth)
                            .First()
                    )
                    .ToList();
            }

            if (request.Year.HasValue || request.Month.HasValue)
            {
                _logger.LogInformation("Filtering operations by year {Year} and month {Month}.", request.Year, request.Month);

                operationsQuery = operationsQuery
                    .Where(o =>
                        (!request.Year.HasValue || o.Created.Year == request.Year.Value) &&
                        (!request.Month.HasValue || o.Created.Month == request.Month.Value)
                    );
            }

            userDashVm.Email = user.Email;
            userDashVm.Nom = user.Nom;
            userDashVm.Role = isAgent ? Roles.Agent : Roles.Client;
            userDashVm.UserName = user.UserName;
            userDashVm.CodeUser = isClient ? user.CodeRef : null;
            userDashVm.NbrTotalOperations = await operationsQuery.CountAsync(cancellationToken);
            userDashVm.NbrEncoursOperations = await operationsQuery
                .Where(o => o.EtatOperation != EtatOperation.cloture)
                .CountAsync(cancellationToken);
            userDashVm.NbrTotalExportOperations = await operationsQuery
                .Where(o => o.TypeOperation == TypeOperation.Export)
                .CountAsync(cancellationToken);
            userDashVm.NbrTotalImportOperations = await operationsQuery
                .Where(o => o.TypeOperation == TypeOperation.Import)
                .CountAsync(cancellationToken);

            var etatOperationsList = Enum.GetValues(typeof(EtatOperation))
                .Cast<EtatOperation>()
                .Select(p => new EtatOperationDto { Value = (int)p, Name = p.ToString() })
                .ToList();

            foreach (var etat in etatOperationsList)
            {
                var count = await operationsQuery
                    .Where(o => (int)o.EtatOperation == etat.Value)
                    .CountAsync(cancellationToken);

                userDashVm.OperationEtatDtos.Add(new OperationEtatDto
                {
                    Etat = etat.Name,
                    NumberOfOperations = count
                });
            }

            _logger.LogInformation("User details successfully created for user ID {UserId}.", user.Id);

            return userDashVm;
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {UserId} not found.", request.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling GetUserDetailsQuery for user ID {UserId}.", request.Id);
            throw new ApplicationException($"An unexpected error occurred while processing the request for user ID {request.Id}.", ex);
        }
    }

}
