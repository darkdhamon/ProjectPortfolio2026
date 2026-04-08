using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class PortfolioContactMethodConfiguration : IEntityTypeConfiguration<PortfolioContactMethod>
{
    public void Configure(EntityTypeBuilder<PortfolioContactMethod> builder)
    {
        builder.ToTable("PortfolioContactMethods");

        builder.Property(contactMethod => contactMethod.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(contactMethod => contactMethod.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(contactMethod => contactMethod.Value)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(contactMethod => contactMethod.Href)
            .HasMaxLength(500);

        builder.Property(contactMethod => contactMethod.Note)
            .HasMaxLength(500);

        builder.Property(contactMethod => contactMethod.IsVisible)
            .HasDefaultValue(true);
    }
}
