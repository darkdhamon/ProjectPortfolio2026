using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class PortfolioSocialLinkConfiguration : IEntityTypeConfiguration<PortfolioSocialLink>
{
    public void Configure(EntityTypeBuilder<PortfolioSocialLink> builder)
    {
        builder.ToTable("PortfolioSocialLinks");

        builder.Property(socialLink => socialLink.Platform)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(socialLink => socialLink.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(socialLink => socialLink.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(socialLink => socialLink.Handle)
            .HasMaxLength(150);

        builder.Property(socialLink => socialLink.Summary)
            .HasMaxLength(500);

        builder.Property(socialLink => socialLink.IsVisible)
            .HasDefaultValue(true);
    }
}
