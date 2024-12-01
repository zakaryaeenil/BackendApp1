using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Identity;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class HistoriqueConfiguration : IEntityTypeConfiguration<Historique>
    {
        public void Configure(EntityTypeBuilder<Historique> builder)
        {
            // Configure primary key
            builder.HasKey(c => c.Id);

            // Configure properties
            builder.Property(c => c.UserId)
                .HasMaxLength(450) // Assuming the UserId corresponds to a GUID or string-based ID
                .IsRequired();     // Required field

            builder.Property(c => c.OperationId)
                .IsRequired();     // Required field

            builder.Property(c => c.Action)
                .HasMaxLength(1000) // Maximum length for the message
                .IsRequired();      // Required field

            // Configure relationships
            builder.HasOne<Operation>()
                .WithMany(d => d.Historiques)
                .HasForeignKey(c => c.OperationId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete with Operation
            
             // Configure relationships
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete with Operation
        }
    }
}
