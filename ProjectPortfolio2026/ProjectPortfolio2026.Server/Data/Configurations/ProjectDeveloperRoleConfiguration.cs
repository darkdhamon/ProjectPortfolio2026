using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class ProjectDeveloperRoleConfiguration : IEntityTypeConfiguration<ProjectDeveloperRole>
{
    public void Configure(EntityTypeBuilder<ProjectDeveloperRole> builder)
    {
        builder.ToTable("ProjectDeveloperRoles");

        builder.Property(role => role.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
