using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class OperationDetailVm
{
    public IReadOnlyCollection<CommentaireDto> Commentaires { get; init; } = [];
    public IReadOnlyCollection<DocumentDto> Documents { get; init; } = [];
    public object Operation { get; init; } = new object();
}