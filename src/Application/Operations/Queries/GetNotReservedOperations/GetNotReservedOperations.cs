﻿using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Mappings;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.GetNotReservedOperations;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetNotReservedOperationsQuery : IRequest<PaginatedList<OperationDto>>
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

public class GetNotReservedOperationsQueryValidator : AbstractValidator<GetNotReservedOperationsQuery>
{
    public GetNotReservedOperationsQueryValidator()
    {
         RuleFor(x => x.PageNumber)
           .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

public class GetNotReservedOperationsQueryHandler : IRequestHandler<GetNotReservedOperationsQuery, PaginatedList<OperationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetNotReservedOperationsQueryHandler> _logger;


    public GetNotReservedOperationsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ILogger<GetNotReservedOperationsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<PaginatedList<OperationDto>> Handle(GetNotReservedOperationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetNotReservedOperationsQuery received with parameters: {Request}", request);

        if (string.IsNullOrWhiteSpace(_currentUserService.Id))
        {
            _logger.LogError("Unauthorized access attempt. Current user ID is null or empty.");
            throw new UnauthorizedAccessException("User is not authorized.");
        }

        // Validate client role
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
             operationsQuery = !string.IsNullOrWhiteSpace(request.RechercheId)
                                    ?
                                    operationsQuery
                                    .Where(o => !o.EstReserver && o.EtatOperation != EtatOperation.cloture && o.Id.ToString().Contains(request.RechercheId))
                                    .AsNoTracking()
                                    :
                                    operationsQuery
                                    .Where(o => !o.EstReserver && o.EtatOperation != EtatOperation.cloture)
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
                return new PaginatedList<OperationDto>(Array.Empty<OperationDto>(), 0, request.PageNumber, request.PageSize);
            }

            // Paginate and project to DTO
            PaginatedList<OperationDto> paginatedList = await operationsQuery
                  .OrderByDescending(t => t.LastModified)
            .ThenBy(t => !t.EstReserver)
                 .ThenBy(t => t.EtatOperation != EtatOperation.cloture)
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
