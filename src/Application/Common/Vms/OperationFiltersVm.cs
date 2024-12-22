
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class OperationFiltersVm
{
    public IReadOnlyCollection<TypeOperationDto> TypeOperations { get; init; } = Array.Empty<TypeOperationDto>();
    public IReadOnlyCollection<EtatOperationDto> EtatOperations { get; init; } = Array.Empty<EtatOperationDto>();
    public ICollection<UserDto> ListAgents { get; init; } = Array.Empty<UserDto>();
    public ICollection<UserDto> ListClients { get; init; } = Array.Empty<UserDto>();
    public IReadOnlyCollection<DossierHelpersDto> ListDossierHelpersDto { get; init; } = Array.Empty<DossierHelpersDto>();
}