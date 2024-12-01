namespace NejPortalBackend.Domain.Entities;

public class Historique : BaseAuditableEntity
{
   public required string UserId { get; set; }
   public required int OperationId { get; set; }
   public required string Action  { get; set; }
}
