using System;
namespace NejPortalBackend.Domain.Entities
{
    public class Regime : BaseAuditableEntity
    {
        public required string CodeRegime { get; set; }
        public required TypeOperation TypeOperation { get; set; }

    }
}

