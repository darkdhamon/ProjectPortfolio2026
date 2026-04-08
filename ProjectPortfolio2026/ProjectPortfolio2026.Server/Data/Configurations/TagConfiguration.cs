using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectPortfolio2026.Server.Domain.Tags;

namespace ProjectPortfolio2026.Server.Data.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.Property(tag => tag.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tag => tag.NormalizedName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(tag => new { tag.Category, tag.NormalizedName })
            .IsUnique();
    }
}
