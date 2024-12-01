using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class ExportOperationsVm
{
    public byte[]? FileContent { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}