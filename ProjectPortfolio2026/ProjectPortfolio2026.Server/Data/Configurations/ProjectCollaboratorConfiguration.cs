using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectCollaboratorConfiguration : IEntityTypeConfiguration<ProjectCollaborator>
{
    public void Configure(EntityTypeBuilder<ProjectCollaborator> builder)
    {
        builder.ToTable("ProjectCollaborators");

        builder.Property(collaborator => collaborator.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(collaborator => collaborator.GitHubProfileUrl)
            .HasMaxLength(500);

        builder.Property(collaborator => collaborator.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(collaborator => collaborator.PhotoUrl)
            .HasMaxLength(500);

        builder.HasMany(collaborator => collaborator.Roles)
            .WithOne(role => role.ProjectCollaborator)
            .HasForeignKey(role => role.ProjectCollaboratorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
