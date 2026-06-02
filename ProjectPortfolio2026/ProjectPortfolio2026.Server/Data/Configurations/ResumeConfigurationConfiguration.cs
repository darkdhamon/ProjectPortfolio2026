using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ResumeConfigurationConfiguration : IEntityTypeConfiguration<ResumeConfiguration>
{
    public void Configure(EntityTypeBuilder<ResumeConfiguration> builder)
    {
        builder.ToTable("ResumeConfigurations");

        builder.Property(configuration => configuration.SourceType)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(ResumeSourceTypes.None);

        builder.Property(configuration => configuration.SourceUrl)
            .HasMaxLength(2048);

        builder.Property(configuration => configuration.DisplayLabel)
            .HasMaxLength(120);

        builder.Property(configuration => configuration.Summary)
            .HasMaxLength(280);
    }
}
