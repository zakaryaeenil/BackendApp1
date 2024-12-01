

namespace NejPortalBackend.Application.Common.Vms;
public class DownloadDocumentVm
{
    public byte[]? FileContent { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }

}