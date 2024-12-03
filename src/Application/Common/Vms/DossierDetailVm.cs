using System;
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;

public class DossierDetailVm
{
    public IReadOnlyCollection<FactureDto> FactureDtos { get; init; } = Array.Empty<FactureDto>();
    public IReadOnlyCollection<OperationDto> Operations { get; init; } = Array.Empty<OperationDto>();
    public DossierDto Dossier { get; init; } = new DossierDto();
}

