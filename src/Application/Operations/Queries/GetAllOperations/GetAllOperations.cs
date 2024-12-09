using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.GetAllOperations;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetAllOperationsQuery : IRequest<PaginatedList<OperationDto>>
{
    public int? TypeOpration { get; init; }
    public IList<int>? EtatOprations { get; init; }
    public IList<string>? Clients { get; init; }
    public IList<string>? Agents { get; init; }
    public string? RechercheId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public bool InClients { get; init; }
    public bool InEtatOprations { get; init; }
    public bool InAgents { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAllOperationsQueryValidator : AbstractValidator<GetAllOperationsQuery>
{
    public GetAllOperationsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
           .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

public class GetAllOperationsQueryHandler : IRequestHandler<GetAllOperationsQuery, PaginatedList<OperationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetAllOperationsQueryHandler> _logger;


    public GetAllOperationsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<GetAllOperationsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<PaginatedList<OperationDto>> Handle(GetAllOperationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllOperationsQuery received with parameters: {Request}", request);

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate  role
        bool isValidUser = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator) || await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
        if (!isValidUser)
        {
            _logger.LogWarning("User {UserId} attempted to access equipe operations without proper role.", _currentUserService.Id);
            throw new InvalidOperationException($"User {_currentUserService.Id} does not have the required role.");
        }

        try
        {
            bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
            bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

            IQueryable<Operation> operationsQuery = _context.Operations.AsNoTracking();
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

            if (request.Agents?.Count > 0)
                operationsQuery = request.InAgents
                    ? operationsQuery.Where(o => !string.IsNullOrWhiteSpace(o.ReserverPar) && request.Agents.Contains(o.ReserverPar))
                    : operationsQuery.Where(o => string.IsNullOrWhiteSpace(o.ReserverPar) || (!string.IsNullOrWhiteSpace(o.ReserverPar) && !request.Agents.Contains(o.ReserverPar)));

            int totalCount = await operationsQuery.CountAsync(cancellationToken);
            if (totalCount == 0)
            {
                _logger.LogInformation("No operations found for user {UserId} with the given filters.", _currentUserService.Id);
            
            }

            // Paginate and project to DTO
            PaginatedList<OperationDto> paginatedList = await operationsQuery
                .OrderByDescending(t => t.EtatOperation != EtatOperation.cloture)
                 .ThenBy(t => !t.EstReserver)
                  .ThenBy(t => t.LastModified)                
                     .ThenBy(t => t.EtatOperation != EtatOperation.cloture)
                     .ProjectTo<OperationDto>(_mapper.ConfigurationProvider)
                    .PaginatedListAsync(request.PageNumber, request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} operations for user {UserId}.", paginatedList.Items.Count, _currentUserService.Id);

            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while processing GetAllOperationsQuery for user {UserId}.", _currentUserService.Id);
            throw;
        }
    }
}
