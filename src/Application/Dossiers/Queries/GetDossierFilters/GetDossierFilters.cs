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

    public GetDossierFiltersQueryHandler(
        IIdentityService identityService,
        ILogger<GetDossierFiltersQueryHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<DossierFiltersVm> Handle(GetDossierFiltersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetDossierFiltersQuery.");

        try
        {
            var etatPayments = Enum.GetValues(typeof(EtatPayement))
                .Cast<EtatPayement>()
                .Select(p => new EtatPaymentDto { Value = (int)p, Name = p.ToString() })
                .ToList();
            _logger.LogDebug("Retrieved {Count} payment states.", etatPayments.Count);

            var listAgents = await _identityService.GetAllUsersInRoleAsync(Roles.Agent);
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
