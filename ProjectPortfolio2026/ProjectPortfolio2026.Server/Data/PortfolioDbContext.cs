using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();

    public DbSet<ProjectCollaboratorRole> ProjectCollaboratorRoles => Set<ProjectCollaboratorRole>();

    public DbSet<ProjectDeveloperRole> ProjectDeveloperRoles => Set<ProjectDeveloperRole>();

    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();

    public DbSet<ProjectScreenshot> ProjectScreenshots => Set<ProjectScreenshot>();

    public DbSet<ProjectSkill> ProjectSkills => Set<ProjectSkill>();

    public DbSet<ProjectTechnology> ProjectTechnologies => Set<ProjectTechnology>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}
