using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class JobRoleConfiguration : IEntityTypeConfiguration<JobRole>
{
    public void Configure(EntityTypeBuilder<JobRole> builder)
    {
        builder.ToTable("JobRoles");

        builder.Property(jobRole => jobRole.Role)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(jobRole => jobRole.SupervisorName)
            .HasMaxLength(200);

        builder.Property(jobRole => jobRole.DescriptionMarkdown)
            .IsRequired();
    }
}
