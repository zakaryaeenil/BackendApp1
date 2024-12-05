using System;
namespace NejPortalBackend.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public required string UserId { get; set; }
    public bool IsRead { get; set; } = false;
    public required string Message { get; set; }
}

