using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.GetDossierFilters;

[Authorize(Roles = Roles.AdminAndAgent)]
public record GetDossierFiltersQuery : IRequest<DossierFiltersVm>;




public class GetDossierFiltersQueryHandler : IRequestHandler<GetDossierFiltersQuery, DossierFiltersVm>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetDossierFiltersQueryHandler> _logger;
    private readonly IUser _currentUserService;
    public GetDossierFiltersQueryHandler(
        IUser currentUserService,
        IIdentityService identityService,
        ILogger<GetDossierFiltersQueryHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<DossierFiltersVm> Handle(GetDossierFiltersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetDossierFiltersQuery.");
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
            var etatPayments = Enum.GetValues(typeof(EtatPayement))
                .Cast<EtatPayement>()
                .Select(p => new EtatPaymentDto { Value = (int)p, Name = p.ToString() })
                .ToList();
            _logger.LogDebug("Retrieved {Count} payment states.", etatPayments.Count);

            bool isAgent = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Agent);
            bool isAdmin = await _identityService.IsInRoleAsync(_currentUserService.Id, Roles.Administrator);

            int? typeOperation = await _identityService.GetTypeOperationAsync(_currentUserService.Id);

            var listAgents = await _identityService.GetAllUsersInRoleAsync(Roles.Agent);
            if (isAdmin)
            {
                listAgents = typeOperation != null ? listAgents.Where(o => o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : listAgents;
            }
            if (isAgent)
            {
                listAgents = typeOperation != null ? listAgents.Where(o => o.TypeOperation != null && (int)o.TypeOperation == typeOperation).ToList() : throw new UnauthorizedAccessException("User is not authorized.");
            }
            _logger.LogDebug("Retrieved {Count} agents.", listAgents.Count);

            var listClients = await _identityService.GetAllUsersInRoleAsync(Roles.Client);
            _logger.LogDebug("Retrieved {Count} clients.", listClients.Count);

            var result = new DossierFiltersVm
            {
                EtatPayments = etatPayments,
                ListAgents = listAgents,
                ListClients = listClients
            };

            _logger.LogInformation("Successfully handled GetDossierFiltersQuery.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while handling GetDossierFiltersQuery.");
            throw;
        }
    }
}
