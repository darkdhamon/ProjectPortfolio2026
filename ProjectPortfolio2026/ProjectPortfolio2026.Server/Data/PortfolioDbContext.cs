using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Domain.WorkHistory;

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

    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();

    public DbSet<ProjectScreenshot> ProjectScreenshots => Set<ProjectScreenshot>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<Employer> Employers => Set<Employer>();

    public DbSet<JobRole> JobRoles => Set<JobRole>();

    public DbSet<JobRoleTag> JobRoleTags => Set<JobRoleTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}
