using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.HasKey(c => c.Id);
           
            builder.Property(c => c.CodeClient)
                .IsRequired(); // Required field

            builder.Property(c => c.Nom)
                .HasMaxLength(250) // Assuming a max length for the client's name
                .IsRequired(); // Required field

            // Configure additional table settings if needed
            // For example, adding a unique index on 'Nom' if it's unique
            builder.HasIndex(c => c.CodeClient)
                .IsUnique();
        }
    }
}
