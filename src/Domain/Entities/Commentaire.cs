namespace NejPortalBackend.Domain.Entities;

public class Commentaire : BaseAuditableEntity
{
   public required string UserId { get; set; }
   public required int OperationId { get; set; }
   public required string Message  { get; set; }
}
