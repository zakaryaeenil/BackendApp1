using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Application.Operations.Queries.GetAllOperations;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.GetExportCsvOperations;


[Authorize(Roles = Roles.AdminAndAgent)]
public record GetExportCsvOperationsQuery : IRequest<ExportOperationsVm>
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
}

public class GetExportCsvOperationsQueryValidator : AbstractValidator<GetExportCsvOperationsQuery>
{
    public GetExportCsvOperationsQueryValidator()
    {
    }
}

public class GetExportCsvOperationsQueryHandler : IRequestHandler<GetExportCsvOperationsQuery, ExportOperationsVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetExportCsvOperationsQueryHandler> _logger;
    private readonly ICsvExportService _csvExportService;


    public GetExportCsvOperationsQueryHandler(
        IApplicationDbContext context,
        IUser currentUserService,
        IIdentityService identityService,
        IMapper mapper,
        ICsvExportService csvExportService,
        ILogger<GetExportCsvOperationsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _csvExportService = csvExportService;
        _logger = logger;
    }


    public async Task<ExportOperationsVm> Handle(GetExportCsvOperationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetExportCsvOperationsQuery received with parameters: {Request}", request);

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
            int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

            IQueryable<Operation> operationsQuery = _context.Operations.AsNoTracking();
            // Filter operations by user and criteria
            if (isAdmin)
            {
                operationsQuery = typeOperation != null ? operationsQuery.Where(o => (int)o.TypeOperation == typeOperation) : operationsQuery;
            }
            if (isAgent)
            {
                operationsQuery = typeOperation != null ? operationsQuery.Where(o => (int)o.TypeOperation == typeOperation) : throw new UnauthorizedAccessException("User is not authorized.");
            }
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

            if (request.TypeOpration.HasValue && isAdmin && typeOperation == null)
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

            // Fetch operations and related data
            var operations = await operationsQuery.ToListAsync(cancellationToken);

            // Fetch usernames in batch to avoid multiple async calls in LINQ
            var reserverParIds = operations.Where(o =>!string.IsNullOrWhiteSpace(o.ReserverPar)).Select(o => o.ReserverPar).Distinct();
            var userIds = operations.Select(o => o.UserId).Distinct();
            var allUserIds = reserverParIds.Concat(userIds).Distinct();

            var userNames = new Dictionary<string, string>();
            foreach (var userId in allUserIds)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    userNames[userId] = await _identityService.GetUserNameAsync(userId) ?? string.Empty;
                }
            }

            // Project to DTO
            var listOperations = operations.Select(o => new ExportCsvOperationDto
            {
                Id = o.Id,
                EtatOperation = Enum.GetName(typeof(EtatOperation), (int)o.EtatOperation) ?? "Unknown",
                TypeOperation = Enum.GetName(typeof(TypeOperation), (int)o.TypeOperation) ?? "Unknown",
                ReserverPar = !string.IsNullOrWhiteSpace(o.ReserverPar) && userNames.ContainsKey(o.ReserverPar) ? userNames[o.ReserverPar] : string.Empty,
                UserId = userNames.ContainsKey(o.UserId) ? userNames[o.UserId] : string.Empty,
                nbrDocs = o.Documents.Count,
                Regime = o.Regime,
                Bureau = o.Bureau,
                CodeDossier = o.CodeDossier,
                Created = o.Created,
                LastModified = o.LastModified
            });
            _logger.LogInformation("Successfully retrieved operations for user {UserId}.",  _currentUserService.Id);

            var csvContent =  _csvExportService.ExportToCsv<ExportCsvOperationDto>(listOperations);
           

            if (string.IsNullOrWhiteSpace(csvContent))
                throw new InvalidOperationException($"problem export methode");

            var bytes = Encoding.UTF8.GetBytes(csvContent);

            // Get today's date in the desired format
            var today = DateTime.Now.ToString("yyyy-MM-dd"); // Format: YYYY-MM-DD

            return new ExportOperationsVm { ContentType = "text/csv", FileContent = bytes, FileName = "Export_Operations_{today}.csv" +today+ ".csv"};
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while processing GetExportCsvOperationsQuery for user {UserId}.", _currentUserService.Id);
            throw;
        }
    }
}
