using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class JobRoleTagConfiguration : IEntityTypeConfiguration<JobRoleTag>
{
    public void Configure(EntityTypeBuilder<JobRoleTag> builder)
    {
        builder.ToTable("JobRoleTags");
        builder.HasKey(jobRoleTag => new { jobRoleTag.JobRoleId, jobRoleTag.TagId });

        builder.HasOne(jobRoleTag => jobRoleTag.JobRole)
            .WithMany(parentJobRole => parentJobRole.JobRoleTags)
            .HasForeignKey(jobRoleTag => jobRoleTag.JobRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(jobRoleTag => jobRoleTag.Tag)
            .WithMany(tag => tag.JobRoleTags)
            .HasForeignKey(jobRoleTag => jobRoleTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
