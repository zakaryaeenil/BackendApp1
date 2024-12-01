using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class FactureConfiguration : IEntityTypeConfiguration<Facture>
    {
        public void Configure(EntityTypeBuilder<Facture> builder)
        {
            // Configure primary key
            builder.HasKey(f => f.Id);


            builder.Property(f => f.CodeFacture)
                .HasMaxLength(100) // Assuming a max length for the facture code
                .IsRequired(false); // Nullable property

            builder.Property(f => f.CodeDossier)
                .HasMaxLength(100) // Assuming a max length for the dossier code
                .IsRequired(false); // Required property

            builder.Property(f => f.DateEcheance)
                .IsRequired(false); // Required field

            builder.Property(f => f.DateEmission)
                .IsRequired(false); // Required field

            builder.Property(f => f.MontantTotal)
                  .HasPrecision(18, 2) // Configure precision for monetary values
                .IsRequired(false);

            builder.Property(f => f.MontantRestant)
                  .HasPrecision(18, 2) // Configure precision for monetary values
                .IsRequired(false);

            builder.Property(f => f.MontantPaye)
                  .HasPrecision(18, 2) // Configure precision for monetary values
                .IsRequired(false);

            builder.Property(f => f.Devise)
                .HasMaxLength(10) // Assuming a short code for currency (e.g., USD, EUR)
                .IsRequired(false);

            builder.Property(f => f.Description)
                .HasMaxLength(1000) // Description length
                .IsRequired(false);

            builder.Property(f => f.CheminFichier)
                .HasMaxLength(500) // File path length
                .IsRequired(false);

            builder.Property(f => f.MethodePaiement)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(f => f.InstructionsPaiement)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(f => f.CodeClient)
                .IsRequired(false);

                 // Configure relationships
            builder.Property(d => d.EtatPayement)
               .IsRequired(); // Prevent cascading delete

        }
    }
}
