﻿using System.Data;
using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Dossiers.Queries.ClientGetDossierFilters;

[Authorize(Roles = Roles.Client)]
public record ClientGetDossierFiltersQuery : IRequest<DossierFiltersVm>;

public class ClientGetDossierFiltersQueryHandler : IRequestHandler<ClientGetDossierFiltersQuery, DossierFiltersVm>
{
    private readonly ILogger<ClientGetDossierFiltersQueryHandler> _logger;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUserService;

    public ClientGetDossierFiltersQueryHandler(
          IUser currentUserService,
        IIdentityService identityService,
        ILogger<ClientGetDossierFiltersQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _identityService = identityService;
    }

    public async Task<DossierFiltersVm> Handle(ClientGetDossierFiltersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetDossierFiltersQueryHandler received with parameters: {Request}", request);

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
            throw new InvalidOperationException("User " + _currentUserService.Id + " does not have the required role.");
        }
        try
        {
            var etatPayments = Enum.GetValues(typeof(EtatPayement))
                .Cast<EtatPayement>()
                .Select(p => new EtatPaymentDto { Value = (int)p, Name = p.ToString() })
                .ToList();
            _logger.LogDebug("Retrieved {Count} payment states.", etatPayments.Count);

            var result = new DossierFiltersVm
            {
                EtatPayments = etatPayments
            };

            _logger.LogInformation("Successfully handled ClientGetDossierFiltersQueryHandler.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "An error occurred while handling ClientGetDossierFiltersQueryHandler.");
            throw;
        }
    }
}
