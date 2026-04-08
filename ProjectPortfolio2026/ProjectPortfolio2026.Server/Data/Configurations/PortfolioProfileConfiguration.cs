using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class PortfolioProfileConfiguration : IEntityTypeConfiguration<PortfolioProfile>
{
    public void Configure(EntityTypeBuilder<PortfolioProfile> builder)
    {
        builder.ToTable("PortfolioProfiles");

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.ContactHeadline)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(profile => profile.ContactIntro)
            .IsRequired();

        builder.Property(profile => profile.AvailabilityHeadline)
            .HasMaxLength(200);

        builder.Property(profile => profile.AvailabilitySummary)
            .HasMaxLength(500);

        builder.Property(profile => profile.IsPublic)
            .HasDefaultValue(false);

        builder.HasMany(profile => profile.ContactMethods)
            .WithOne(contactMethod => contactMethod.PortfolioProfile)
            .HasForeignKey(contactMethod => contactMethod.PortfolioProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.SocialLinks)
            .WithOne(socialLink => socialLink.PortfolioProfile)
            .HasForeignKey(socialLink => socialLink.PortfolioProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
