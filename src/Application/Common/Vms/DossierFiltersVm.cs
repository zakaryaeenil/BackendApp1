using System;
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;

public class DossierFiltersVm
{
    public IReadOnlyCollection<EtatPaymentDto> EtatPayments { get; init; } = Array.Empty<EtatPaymentDto>();
    public ICollection<UserDto> ListAgents { get; init; } = Array.Empty<UserDto>();
    public ICollection<UserDto> ListClients { get; init; } = Array.Empty<UserDto>();
}

