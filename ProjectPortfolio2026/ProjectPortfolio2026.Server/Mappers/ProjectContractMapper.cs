using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;

namespace ProjectPortfolio2026.Server.Mappers;

public static class ProjectContractMapper
{
    public static Project ToDomain(this ProjectRequest request)
    {
        return new Project
        {
            Title = request.Title,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PrimaryImageUrl = request.PrimaryImageUrl,
            ShortDescription = request.ShortDescription,
            LongDescriptionMarkdown = request.LongDescriptionMarkdown,
            GitHubUrl = request.GitHubUrl,
            DemoUrl = request.DemoUrl,
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured,
            Screenshots = (request.Screenshots ?? [])
                .Select(screenshot => new ProjectScreenshot
                {
                    ImageUrl = screenshot.ImageUrl,
                    Caption = screenshot.Caption,
                    SortOrder = screenshot.SortOrder
                })
                .ToList(),
            DeveloperRoles = (request.DeveloperRoles ?? [])
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => new ProjectDeveloperRole { Name = role.Trim() })
                .ToList(),
            ProjectTags = CreateProjectTags(request),
            Collaborators = (request.Collaborators ?? [])
                .Select(collaborator => new ProjectCollaborator
                {
                    Name = collaborator.Name,
                    GitHubProfileUrl = collaborator.GitHubProfileUrl,
                    WebsiteUrl = collaborator.WebsiteUrl,
                    PhotoUrl = collaborator.PhotoUrl,
                    Roles = (collaborator.Roles ?? [])
                        .Where(role => !string.IsNullOrWhiteSpace(role))
                        .Select(role => new ProjectCollaboratorRole { Name = role.Trim() })
                        .ToList()
                })
                .ToList(),
            Milestones = (request.Milestones ?? [])
                .Select(milestone => new ProjectMilestone
                {
                    Title = milestone.Title,
                    TargetDate = milestone.TargetDate,
                    CompletedOn = milestone.CompletedOn,
                    Description = milestone.Description
                })
                .ToList()
        };
    }

    public static void ApplyTo(this ProjectRequest request, Project project)
    {
        var updatedProject = request.ToDomain();

        project.Title = updatedProject.Title;
        project.StartDate = updatedProject.StartDate;
        project.EndDate = updatedProject.EndDate;
        project.PrimaryImageUrl = updatedProject.PrimaryImageUrl;
        project.ShortDescription = updatedProject.ShortDescription;
        project.LongDescriptionMarkdown = updatedProject.LongDescriptionMarkdown;
        project.GitHubUrl = updatedProject.GitHubUrl;
        project.DemoUrl = updatedProject.DemoUrl;
        project.IsPublished = updatedProject.IsPublished;
        project.IsFeatured = updatedProject.IsFeatured;
        project.Screenshots = updatedProject.Screenshots;
        project.DeveloperRoles = updatedProject.DeveloperRoles;
        project.ProjectTags = updatedProject.ProjectTags;
        project.Collaborators = updatedProject.Collaborators;
        project.Milestones = updatedProject.Milestones;
    }

    public static ProjectResponse ToResponse(this Project project, string? requestId = null)
    {
        return new ProjectResponse
        {
            RequestId = requestId,
            Id = project.Id,
            Title = project.Title,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            PrimaryImageUrl = project.PrimaryImageUrl,
            ShortDescription = project.ShortDescription,
            LongDescriptionMarkdown = project.LongDescriptionMarkdown,
            GitHubUrl = project.GitHubUrl,
            DemoUrl = project.DemoUrl,
            IsPublished = project.IsPublished,
            IsFeatured = project.IsFeatured,
            Screenshots = project.Screenshots
                .OrderBy(screenshot => screenshot.SortOrder)
                .Select(screenshot => new ProjectScreenshotResponse
                {
                    ImageUrl = screenshot.ImageUrl,
                    Caption = screenshot.Caption,
                    SortOrder = screenshot.SortOrder
                })
                .ToList(),
            DeveloperRoles = project.DeveloperRoles
                .Select(role => role.Name)
                .OrderBy(role => role)
                .ToList(),
            Technologies = project.ProjectTags
                .Where(projectTag => projectTag.Tag?.Category == TagCategory.Technology)
                .Select(projectTag => projectTag.Tag!.DisplayName)
                .OrderBy(technology => technology)
                .ToList(),
            Skills = project.ProjectTags
                .Where(projectTag => projectTag.Tag?.Category == TagCategory.Skill)
                .Select(projectTag => projectTag.Tag!.DisplayName)
                .OrderBy(skill => skill)
                .ToList(),
            Collaborators = project.Collaborators
                .OrderBy(collaborator => collaborator.Name)
                .Select(collaborator => new ProjectCollaboratorResponse
                {
                    Name = collaborator.Name,
                    GitHubProfileUrl = collaborator.GitHubProfileUrl,
                    WebsiteUrl = collaborator.WebsiteUrl,
                    PhotoUrl = collaborator.PhotoUrl,
                    Roles = collaborator.Roles
                        .Select(role => role.Name)
                        .OrderBy(role => role)
                        .ToList()
                })
                .ToList(),
            Milestones = project.Milestones
                .OrderBy(milestone => milestone.TargetDate)
                .ThenBy(milestone => milestone.Title)
                .Select(milestone => new ProjectMilestoneResponse
                {
                    Title = milestone.Title,
                    TargetDate = milestone.TargetDate,
                    CompletedOn = milestone.CompletedOn,
                    Description = milestone.Description
                })
                .ToList()
        };
    }

    public static ProjectSummaryResponse ToResponse(this ProjectListItem project, string? requestId = null)
    {
        return new ProjectSummaryResponse
        {
            RequestId = requestId,
            Id = project.Id,
            Title = project.Title,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            PrimaryImageUrl = project.PrimaryImageUrl,
            ShortDescription = project.ShortDescription,
            GitHubUrl = project.GitHubUrl,
            DemoUrl = project.DemoUrl,
            IsFeatured = project.IsFeatured,
            Skills = project.Skills.OrderBy(skill => skill).ToList(),
            Technologies = project.Technologies.OrderBy(technology => technology).ToList()
        };
    }

    private static List<ProjectTag> CreateProjectTags(ProjectRequest request)
    {
        return CreateProjectTags(TagCategory.Technology, request.Technologies)
            .Concat(CreateProjectTags(TagCategory.Skill, request.Skills))
            .ToList();
    }

    private static IEnumerable<ProjectTag> CreateProjectTags(TagCategory category, IEnumerable<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => new ProjectTag
            {
                Tag = new Tag
                {
                    Category = category,
                    DisplayName = value,
                    NormalizedName = NormalizeTagName(value)
                }
            });
    }

    private static string NormalizeTagName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
