using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace NejPortalBackend.Infrastructure.Identity.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Configure properties
            builder.Property(user => user.CodeRef)
                .HasMaxLength(50)
                .IsRequired(false); // Nullable field

            builder.Property(user => user.Nom)
                .HasMaxLength(100)
                .IsRequired(false); // Nullable field

            builder.Property(user => user.Email_Notif)
                .HasMaxLength(100)
                .IsRequired(false); // Nullable field

            builder.Property(user => user.Prenom)
                .HasMaxLength(100)
                .IsRequired(false); // Nullable field

            builder.Property(user => user.HasAccess)
                .IsRequired(); // Required boolean field

            builder.Property(user => user.RefreshToken)
                .IsRequired(false); // Nullable field for storing refresh tokens

            builder.Property(user => user.RefreshTokenExpiryTime)
                .IsRequired(false); // Nullable field for the expiry time of the refresh token
        }
    }
}
