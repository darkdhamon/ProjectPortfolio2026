using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.Property(project => project.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(project => project.ShortDescription)
            .IsRequired()
            .HasMaxLength(350);

        builder.Property(project => project.LongDescriptionMarkdown)
            .IsRequired();

        builder.Property(project => project.PrimaryImageUrl)
            .HasMaxLength(500);

        builder.Property(project => project.GitHubUrl)
            .HasMaxLength(500);

        builder.Property(project => project.DemoUrl)
            .HasMaxLength(500);

        builder.Property(project => project.IsPublished)
            .HasDefaultValue(false);

        builder.Property(project => project.IsFeatured)
            .HasDefaultValue(false);

        builder.HasMany(project => project.Screenshots)
            .WithOne(screenshot => screenshot.Project)
            .HasForeignKey(screenshot => screenshot.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.DeveloperRoles)
            .WithOne(role => role.Project)
            .HasForeignKey(role => role.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.Collaborators)
            .WithOne(collaborator => collaborator.Project)
            .HasForeignKey(collaborator => collaborator.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.Milestones)
            .WithOne(milestone => milestone.Project)
            .HasForeignKey(milestone => milestone.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
