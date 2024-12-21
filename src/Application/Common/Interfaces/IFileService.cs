using Microsoft.AspNetCore.Http;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;

namespace NejPortalBackend.Application.Common.Interfaces;
public interface IFileService
{
    Task<FileInformationDto> Create(IFormFile file, string mainFolderName, string userName, int operationId);
    Task<ExportOperationsVm> GenerateExportFile(IEnumerable<OperationDto> operations, bool isCsvExport,DateTime dateTime);
}