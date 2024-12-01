using Microsoft.Extensions.Logging;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Security;
using NejPortalBackend.Application.Common.Vms;
using NejPortalBackend.Domain.Constants;
using NejPortalBackend.Domain.Enums;

namespace NejPortalBackend.Application.Operations.Queries.ClientGetOperationFilters;

[Authorize(Roles = Roles.Client)]
public record ClientGetOperationFiltersQuery : IRequest<OperationFiltersVm>;


public class ClientGetOperationFiltersQueryHandler : IRequestHandler<ClientGetOperationFiltersQuery, OperationFiltersVm>
{
    private readonly ILogger<ClientGetOperationFiltersQueryHandler> _logger;
    public ClientGetOperationFiltersQueryHandler(ILogger<ClientGetOperationFiltersQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<OperationFiltersVm> Handle(ClientGetOperationFiltersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ClientGetOperationFiltersQuery received.");

        try
        {
            var etatOperations = Enum.GetValues(typeof(EtatOperation))
                .Cast<EtatOperation>()
                .Select(p => new EtatOperationDto { Value = (int)p, Name = p.ToString() })
                .ToList();

            var typeOperations = Enum.GetValues(typeof(TypeOperation))
                .Cast<TypeOperation>()
                .Select(p => new TypeOperationDto { Value = (int)p, Name = p.ToString() })
                .ToList();

            var filtersVm = new OperationFiltersVm
            {
                EtatOperations = etatOperations,
                TypeOperations = typeOperations
            };

            _logger.LogInformation("Successfully retrieved operation filters.");
            return await Task.FromResult(filtersVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing ClientGetOperationFiltersQuery.");
            throw;
        }
    }
}
