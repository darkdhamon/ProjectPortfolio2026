using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Domain.Projects;

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
            Screenshots = request.Screenshots
                .Select(screenshot => new ProjectScreenshot
                {
                    ImageUrl = screenshot.ImageUrl,
                    Caption = screenshot.Caption,
                    SortOrder = screenshot.SortOrder
                })
                .ToList(),
            DeveloperRoles = request.DeveloperRoles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => new ProjectDeveloperRole { Name = role.Trim() })
                .ToList(),
            Technologies = request.Technologies
                .Where(technology => !string.IsNullOrWhiteSpace(technology))
                .Select(technology => new ProjectTechnology { Name = technology.Trim() })
                .ToList(),
            Skills = request.Skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => new ProjectSkill { Name = skill.Trim() })
                .ToList(),
            Collaborators = request.Collaborators
                .Select(collaborator => new ProjectCollaborator
                {
                    Name = collaborator.Name,
                    GitHubProfileUrl = collaborator.GitHubProfileUrl,
                    WebsiteUrl = collaborator.WebsiteUrl,
                    PhotoUrl = collaborator.PhotoUrl,
                    Roles = collaborator.Roles
                        .Where(role => !string.IsNullOrWhiteSpace(role))
                        .Select(role => new ProjectCollaboratorRole { Name = role.Trim() })
                        .ToList()
                })
                .ToList(),
            Milestones = request.Milestones
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
        project.Technologies = updatedProject.Technologies;
        project.Skills = updatedProject.Skills;
        project.Collaborators = updatedProject.Collaborators;
        project.Milestones = updatedProject.Milestones;
    }

    public static ProjectResponse ToResponse(this Project project)
    {
        return new ProjectResponse
        {
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
            Technologies = project.Technologies
                .Select(technology => technology.Name)
                .OrderBy(technology => technology)
                .ToList(),
            Skills = project.Skills
                .Select(skill => skill.Name)
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
}
