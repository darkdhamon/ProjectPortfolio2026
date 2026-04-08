using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    public void Configure(EntityTypeBuilder<ProjectTag> builder)
    {
        builder.ToTable("ProjectTags");
        builder.HasKey(projectTag => new { projectTag.ProjectId, projectTag.TagId });

        builder.HasOne(projectTag => projectTag.Project)
            .WithMany(project => project.ProjectTags)
            .HasForeignKey(projectTag => projectTag.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(projectTag => projectTag.Tag)
            .WithMany(tag => tag.ProjectTags)
            .HasForeignKey(projectTag => projectTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
