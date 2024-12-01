
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;

namespace NejPortalBackend.Application.Operations.Queries.ClientGetAllOperations;

[Authorize(Roles = Roles.Client)]
public record ClientGetAllOperationsQuery : IRequest<PaginatedList<ClientOperationDto>>
{
    public int? TypeOpration { get; init; }
    public List<int>? EtatOprations { get; init; }
    public string? RechercheId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public bool InEtatOprations { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class ClientGetAllOperationsQueryValidator : AbstractValidator<ClientGetAllOperationsQuery>
{
    public ClientGetAllOperationsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

public class ClientGetAllOperationsQueryHandler : IRequestHandler<ClientGetAllOperationsQuery, PaginatedList<ClientOperationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<ClientGetAllOperationsQueryHandler> _logger;


    public ClientGetAllOperationsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<ClientGetAllOperationsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<PaginatedList<ClientOperationDto>> Handle(ClientGetAllOperationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetAllOperationsQuery received with parameters: {Request}", request);

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

        try
        {
            // Filter operations by user and criteria
            var operationsQuery = _context.Operations
                .Where(t => t.UserId == _currentUserService.Id)
                .AsNoTracking();

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

            var totalCount = await operationsQuery.CountAsync(cancellationToken);
            if (totalCount == 0)
            {
                _logger.LogInformation("No operations found for user {UserId} with the given filters.", _currentUserService.Id);
                return new PaginatedList<ClientOperationDto>([], 0, request.PageNumber, request.PageSize);
            }

            // Paginate and project to DTO
            var paginatedList = await operationsQuery
                .OrderByDescending(t => t.LastModified)
                .ProjectTo<ClientOperationDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} operations for user {UserId}.", paginatedList.Items.Count, _currentUserService.Id);

            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing ClientGetAllOperationsQuery for user {UserId}.", _currentUserService.Id);
            throw;
        }
    }

}
