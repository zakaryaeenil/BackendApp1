namespace NejPortalBackend.Application.Common.Models;
public class DashboardHelpers
{
    public class OperationTypeDto
    {
        public string? Type { get; init; }
        public int NumberOfOperations { get; init; } = 0;
    }
    public class OperationEtatDto
    {
        public string? Etat { get; init; }
        public int NumberOfOperations { get; init; } = 0;
    }
  //  public class OperationClientDto
    //{
      //  public string? Client { get; init; }
        //public int NumberOfOperations { get; init; } = 0;
       // public IReadOnlyCollection<ChartOperationByYear> ChartClientOperationByYearDtos { get; init; } = Array.Empty<ChartOperationByYear>();
    //}
    //public class OperationAgentDto
    //{
      //  public string? Agent { get; init; }
       // public int NumberOfOperations { get; init; } = 0;
       // public IReadOnlyCollection<ChartOperationByYear> ChartAgentOperationsByYear { get; init; } = Array.Empty<ChartOperationByYear>();
    //}
  

    public class ChartOperationByYear
    {
        public string? Month { get; init; }
        public int NumberTotalOfOperations { get; init; } = 0;
        public int NumberImportOfOperations { get; init; } = 0;
        public int NumberExportOfOperations { get; init; } = 0;
    }
    public class FactureEtatDto
    {
        public string? Etat { get; init; }
        public int NumberOfFactures { get; init; } = 0;
    }
}