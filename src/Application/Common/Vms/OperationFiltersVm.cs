
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class OperationFiltersVm
{
    public IReadOnlyCollection<TypeOperationDto> TypeOperations { get; init; } = [];
    public IReadOnlyCollection<EtatOperationDto> EtatOperations { get; init; } = [];
    public IReadOnlyCollection<UserDto> ListAgents { get; init; } = [];
    public IReadOnlyCollection<UserDto> ListClients { get; init; } = [];
}