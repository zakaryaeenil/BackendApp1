using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Infrastructure.Data.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            // Configure primary key
            builder.HasKey(d => d.Id); // Assuming BaseAuditableEntity has an Id property

            // Configure properties
            builder.Property(d => d.NomDocument)
                .IsRequired() // This is a required field
                .HasMaxLength(100); // Set a max length for file path if needed
            builder.Property(d => d.CheminFichier)
                .IsRequired() // This is a required field
                .HasMaxLength(500); // Set a max length for file path if needed

            builder.Property(d => d.TailleFichier)
                .IsRequired(false); // Nullable field for file size

            builder.Property(d => d.TypeFichier)
                .HasMaxLength(50) // Limit the file type string length
                .IsRequired(false); // Nullable field for file type (MIME/extension)

            builder.Property(d => d.EstAccepte)
                .IsRequired(); // This is a required boolean field

            // Configure relationships
            builder.HasOne<Operation>() // A Document belongs to one Operation
                .WithMany(d => d.Documents) // A Operation can have many Documents
                .HasForeignKey(d => d.OperationId) // The foreign key in Document
                .OnDelete(DeleteBehavior.Cascade); // Optional: delete Documents when the Operation is deleted
        }
    }
}
