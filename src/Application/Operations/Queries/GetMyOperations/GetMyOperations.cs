using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.GetMyOperations;

[Authorize(Roles = Roles.Agent)]
public record GetMyOperationsQuery : IRequest<PaginatedList<OperationDto>>
{
    public int? TypeOpration { get; init; }
    public IList<int>? EtatOprations { get; init; }
    public IList<string>? Clients { get; init; }
    public string? RechercheId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool InClients { get; init; }
    public bool InEtatOprations { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetMyOperationsQueryValidator : AbstractValidator<GetMyOperationsQuery>
{
    public GetMyOperationsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
           .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

public class GetMyOperationsQueryHandler : IRequestHandler<GetMyOperationsQuery, PaginatedList<OperationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetMyOperationsQueryHandler> _logger;


    public GetMyOperationsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<GetMyOperationsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<PaginatedList<OperationDto>> Handle(GetMyOperationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyOperationsQuery received with parameters: {Request}", request);

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate client role
        bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
        if (!isAgent)
        {
            _logger.LogWarning("User {UserId} attempted to access agent operations without proper role.", _currentUserService.Id);
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required role.");
        }

        try
        {

            var operationsQuery =
                    !string.IsNullOrWhiteSpace(request.RechercheId)
           ?
           _context.Operations
           .Where(o => o.ReserverPar == _currentUserService.Id && o.Id.ToString().Contains(request.RechercheId) && o.EtatOperation != EtatOperation.cloture)
           .AsNoTracking()
           :
           _context.Operations
            .Where(o => o.ReserverPar == _currentUserService.Id && o.EtatOperation != EtatOperation.cloture)
          .AsNoTracking();

            // Filter operations by user and criteria
            if (!string.IsNullOrWhiteSpace(request.RechercheId))
            {
                operationsQuery = operationsQuery.Where(o => o.Id.ToString().Contains(request.RechercheId));
                _logger.LogDebug("Filtered operations by RechercheId: {RechercheId}", request.RechercheId);
            }

            if (request.FromDate.HasValue)
            {
                operationsQuery = operationsQuery.Where(o => o.Created >= request.FromDate.Value);
                _logger.LogDebug("Filtered operations from date: {FromDate}", request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                operationsQuery = operationsQuery.Where(o => o.Created <= request.ToDate.Value);
                _logger.LogDebug("Filtered operations to date: {ToDate}", request.ToDate.Value);
            }

            if (request.TypeOpration.HasValue)
            {
                operationsQuery = operationsQuery.Where(o => (int)o.TypeOperation == request.TypeOpration.Value);
                _logger.LogDebug("Filtered operations by TypeOperation: {TypeOperation}", request.TypeOpration.Value);
            }

            if (request.EtatOprations?.Count > 0)
            {
                operationsQuery = request.InEtatOprations
                    ? operationsQuery.Where(o => request.EtatOprations.Contains((int)o.EtatOperation))
                    : operationsQuery.Where(o => !request.EtatOprations.Contains((int)o.EtatOperation));

                _logger.LogDebug("Filtered operations by EtatOprations: {EtatOprations}, InEtatOprations: {InEtatOprations}",
                    request.EtatOprations, request.InEtatOprations);
            }
            if (request.Clients?.Count > 0)
                operationsQuery = request.InClients
                    ? operationsQuery.Where(o => request.Clients.Contains(o.UserId))
                    : operationsQuery.Where(o => !request.Clients.Contains(o.UserId));


            int totalCount = await operationsQuery.CountAsync(cancellationToken);
            if (totalCount == 0)
            {
                _logger.LogInformation("No operations found for user {UserId} with the given filters.", _currentUserService.Id);
                return new PaginatedList<OperationDto>([], 0, request.PageNumber, request.PageSize);
            }

            // Paginate and project to DTO
            PaginatedList<OperationDto> paginatedList = await operationsQuery
                  .OrderByDescending(t => t.LastModified)
            .ThenBy(t => !t.EstReserver)
                .ProjectTo<OperationDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} operations for user {UserId}.", paginatedList.Items.Count, _currentUserService.Id);

            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing GetMyOperationsQuery for user {UserId}.", _currentUserService.Id);
            throw;
        }
    }
}
