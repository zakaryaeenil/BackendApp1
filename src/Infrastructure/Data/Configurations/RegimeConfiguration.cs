using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Infrastructure.Data.Configurations;

public class RegimeConfiguration : IEntityTypeConfiguration<Regime>
{
    public void Configure(EntityTypeBuilder<Regime> builder)
    {
        // Configure primary key
        builder.HasKey(c => c.Id);

        // Configure properties
        builder.Property(c => c.CodeRegime)
            .HasMaxLength(450) 
            .IsRequired();     // Required field

        builder.Property(c => c.TypeOperation)
            .IsRequired();     // Required field
    }
}
