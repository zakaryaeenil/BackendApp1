using System;
using static NejPortalBackend.Application.Common.Models.DashboardHelpers;

namespace NejPortalBackend.Application.Comptes.Queries.GetCompteDetails;

public class CompteDetailsVm
{
    public string? UserName { get; set; }
    public string? CodeUser { get; set; }
    public string? Nom { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public int NbrTotalOperations { get; set; } = 0;
    public int NbrNotReservedOperations { get; set; } = 0;
    public int NbrEncoursOperations { get; set; } = 0;
    public int NbrTotalImportOperations { get; set; } = 0;
    public int NbrTotalExportOperations { get; set; } = 0;
    public int NbrTotalFactures { get; set; } = 0;
    public bool IsClient { get; set; } = false;
    public List<OperationEtatDto> OperationEtatDtos { get; set; } = new List<OperationEtatDto>();
    public List<ChartOperationByYear> ChartOperations { get; set; } = new List<ChartOperationByYear>();


}

