using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class EmployerConfiguration : IEntityTypeConfiguration<Employer>
{
    public void Configure(EntityTypeBuilder<Employer> builder)
    {
        builder.ToTable("Employers");

        builder.Property(employer => employer.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(employer => employer.StreetAddress1)
            .HasMaxLength(200);

        builder.Property(employer => employer.StreetAddress2)
            .HasMaxLength(200);

        builder.Property(employer => employer.City)
            .HasMaxLength(100);

        builder.Property(employer => employer.Region)
            .HasMaxLength(100);

        builder.Property(employer => employer.PostalCode)
            .HasMaxLength(30);

        builder.Property(employer => employer.Country)
            .HasMaxLength(100);

        builder.Property(employer => employer.IsPublished)
            .HasDefaultValue(false);

        builder.HasMany(employer => employer.JobRoles)
            .WithOne(jobRole => jobRole.Employer)
            .HasForeignKey(jobRole => jobRole.EmployerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
