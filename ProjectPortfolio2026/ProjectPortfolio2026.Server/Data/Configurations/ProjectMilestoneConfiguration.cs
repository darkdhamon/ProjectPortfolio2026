using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectMilestoneConfiguration : IEntityTypeConfiguration<ProjectMilestone>
{
    public void Configure(EntityTypeBuilder<ProjectMilestone> builder)
    {
        builder.ToTable("ProjectMilestones");

        builder.Property(milestone => milestone.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(milestone => milestone.Description)
            .HasMaxLength(1000);
    }
}
