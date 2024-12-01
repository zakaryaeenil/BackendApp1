using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class DossierConfiguration : IEntityTypeConfiguration<Dossier>
    {
        public void Configure(EntityTypeBuilder<Dossier> builder)
        {
            // Configure primary key
            builder.HasKey(d => d.Id);

            builder.Property(d => d.CodeClient)
                .HasMaxLength(250) // Assuming a max length for the company name
                .IsRequired(); // Required field

            builder.Property(d => d.Date)
                .IsRequired(); // Required field (if Date is optional, you can remove this)
            
            builder.HasIndex(c => c.CodeDossier)
                .IsUnique();
        }
    }
}
