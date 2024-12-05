using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Identity;

namespace NejPortalBackend.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Configure primary key
        builder.HasKey(c => c.Id);

        // Configure properties
        builder.Property(c => c.UserId)
            .HasMaxLength(450) // Assuming the UserId corresponds to a GUID or string-based ID
            .IsRequired();     // Required field
                               // Configure properties

        builder.Property(c => c.IsRead);     // Required field

        builder.Property(c => c.Message)
            .HasMaxLength(1000) // Maximum length for the message
            .IsRequired();      // Required field
    }
}

