using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectCollaboratorRoleConfiguration : IEntityTypeConfiguration<ProjectCollaboratorRole>
{
    public void Configure(EntityTypeBuilder<ProjectCollaboratorRole> builder)
    {
        builder.ToTable("ProjectCollaboratorRoles");

        builder.Property(role => role.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
