
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class OperationFiltersVm
{
    public IReadOnlyCollection<TypeOperationDto> TypeOperations { get; init; } = Array.Empty<TypeOperationDto>();
    public IReadOnlyCollection<EtatOperationDto> EtatOperations { get; init; } = Array.Empty<EtatOperationDto>();
    public IReadOnlyCollection<UserDto> ListAgents { get; init; } = Array.Empty<UserDto>();
    public IReadOnlyCollection<UserDto> ListClients { get; init; } = Array.Empty<UserDto>();
    public IReadOnlyCollection<DossierHelpersDto> ListDossierHelpersDto { get; init; } = Array.Empty<DossierHelpersDto>();
}