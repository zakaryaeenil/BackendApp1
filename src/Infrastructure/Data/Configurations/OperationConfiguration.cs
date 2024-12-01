using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Identity;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class OperationConfiguration : IEntityTypeConfiguration<Operation>
    {
        public void Configure(EntityTypeBuilder<Operation> builder)
        {
            // Configure the primary key
            builder.HasKey(d => d.Id);
            // Configure properties

            builder.Property(d => d.UserId)
                .IsRequired(); // The UserId is required
            
            builder.Property(d => d.Bureau)
                .HasMaxLength(100)
                .IsRequired(false); // Nullable property
            
            builder.Property(d => d.CodeDossier)
                .HasMaxLength(50)
                .IsRequired(false); // Nullable property
            
            builder.Property(d => d.Regime)
                .HasMaxLength(50)
                .IsRequired(false); // Nullable property
            
            builder.Property(d => d.EstReserver)
                .IsRequired(); // The EstReserver field is required
            
            // Configure 'ReserverPar' as nullable
            builder.Property(d => d.ReserverPar)
                .IsRequired(false); // Nullable field

            // Configure relationships
            builder.Property(d => d.TypeOperation)
               .IsRequired(); // Prevent cascading delete

            // Configure relationships
            builder.Property(d => d.EtatOperation)
               .IsRequired(); // Prevent cascading delete

            builder.HasMany(d => d.Documents)
                .WithOne()
                .HasForeignKey(doc => doc.OperationId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete for documents

            builder.HasMany(d => d.Commentaires)
                .WithOne()
                .HasForeignKey(c => c.OperationId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete for commentaires
              
            builder.HasMany(d => d.Historiques)
                .WithOne()
                .HasForeignKey(h => h.OperationId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete for Historiques
              // Configure relationships
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Cascade delete with Operation
        }
    }
}
