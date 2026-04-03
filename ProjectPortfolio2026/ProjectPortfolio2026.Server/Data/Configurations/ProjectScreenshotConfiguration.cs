using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectScreenshotConfiguration : IEntityTypeConfiguration<ProjectScreenshot>
{
    public void Configure(EntityTypeBuilder<ProjectScreenshot> builder)
    {
        builder.ToTable("ProjectScreenshots");

        builder.Property(screenshot => screenshot.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(screenshot => screenshot.Caption)
            .HasMaxLength(200);
    }
}
