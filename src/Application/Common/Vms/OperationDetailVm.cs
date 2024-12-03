using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Vms;
public class OperationDetailVm
{
    public IReadOnlyCollection<CommentaireDto> Commentaires { get; init; } = Array.Empty<CommentaireDto>();
    public IReadOnlyCollection<DocumentDto> Documents { get; init; } = Array.Empty<DocumentDto>();
    public OperationDto Operation { get; init; } = new OperationDto();
    public ClientOperationDto ClientOperation { get; init; } = new ClientOperationDto();
}