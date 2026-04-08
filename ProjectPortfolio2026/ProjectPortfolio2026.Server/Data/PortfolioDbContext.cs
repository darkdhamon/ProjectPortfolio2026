using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<PortfolioProfile> PortfolioProfiles => Set<PortfolioProfile>();

    public DbSet<PortfolioContactMethod> PortfolioContactMethods => Set<PortfolioContactMethod>();

    public DbSet<PortfolioSocialLink> PortfolioSocialLinks => Set<PortfolioSocialLink>();

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
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}
