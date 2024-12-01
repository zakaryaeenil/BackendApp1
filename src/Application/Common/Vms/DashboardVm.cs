

using static NejPortalBackend.Application.Common.Models.DashboardHelpers;

namespace NejPortalBackend.Application.Common.Vms;
public class DashboardVm
{
        public int NbrNotificationSys { get; set; } = 0;
        public int NbrTotalClients { get; set; } = 0;
        public int NbrTotalAgents { get; set; } = 0;
        public int NbrTotalOperations { get; set; } = 0;
        public int NbrNotReservedOperations { get; set; } = 0;
        public int NbrEncoursOperations { get; set; } = 0;
        public int NbrTotalImportOperations { get; set; } = 0;
        public int NbrTotalExportOperations { get; set; } = 0;
        public int NbrTotalFactures { get; set; } = 0;
        // Use List<T> instead of IReadOnlyCollection<T>
        public List<FactureEtatDto> FactureEtatDtos { get; set; } = [];
        public List<OperationEtatDto> OperationEtatDtos { get; set; }  = [];
        public List<ChartOperationByYear> ChartOperations { get; set; }  = [];

}