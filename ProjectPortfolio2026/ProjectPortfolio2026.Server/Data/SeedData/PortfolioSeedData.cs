using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Data.SeedData;

public static class PortfolioSeedData
{
    public static async Task InitializeAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Projects.AnyAsync(cancellationToken))
        {
            return;
        }

        var projects = CreateProjects();
        dbContext.Projects.AddRange(projects);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Project> CreateProjects()
    {
        var projects = new List<Project>
        {
            CreateProject(
                title: "Project Portfolio 2026",
                startDate: new DateOnly(2026, 1, 10),
                shortDescription: "A recruiter-focused portfolio platform with admin management and analytics foundations.",
                longDescriptionMarkdown: """
                    A full-stack developer portfolio that supports public project discovery, admin editing workflows, and future extensibility for analytics and AI-assisted content experiences.

                    The public experience is designed to make projects easy to browse, compare, and understand without requiring a recruiter or hiring manager to decode vague summaries. Each project detail page is meant to present a clear story with supporting screenshots, collaborators, milestones, and contextual metadata that explain what was built and why it mattered.

                    The admin side of the platform is intended to support ongoing curation instead of treating the portfolio as a static site that has to be hand-edited for every change. That includes structured editing workflows, publishing controls, and a data model that can grow as new sections such as timeline, resume, and blog content are introduced.

                    The current implementation work is also being used as a design and architecture proving ground. It gives space to refine routing, responsive layouts, dark-theme consistency, screenshot presentation, and media-heavy detail views in a way that can scale as the portfolio expands.

                    Longer project narratives like this one are useful for validating real layout behavior. They help test how overview content flows beside a sticky screenshot rail, how section spacing holds up on large screens, and whether the detail page still feels readable when project descriptions are more complete than placeholder seed text.

                    Over time, this project is expected to serve as both a portfolio surface and a reference implementation for maintainable full-stack delivery. That means the codebase needs to support polished presentation, practical content management, and enough flexibility to keep evolving without requiring a redesign every time a new public section is added.
                    """,
                githubUrl: "https://github.com/darkdhamon/ProjectPortfolio2026",
                demoUrl: "https://portfolio2026.local/projects/portfolio-2026",
                developerRoles: ["Lead Developer", "Product Designer"],
                technologies: [".NET 10", "React", "SQL Server"],
                skills: ["API Design", "UI Architecture", "Technical Writing"],
                collaborators:
                [
                    CreateCollaborator("Morgan Lee", "https://github.com/morganlee", "https://morganlee.dev", "https://images.example.test/morgan.png", ["UX Review"]),
                    CreateCollaborator("Taylor Brooks", null, "https://taylorbrooks.dev", null, ["Content Strategy"])
                ],
                milestones:
                [
                    CreateMilestone("Persistence foundation", new DateOnly(2026, 4, 1), "Completed initial project data persistence."),
                    CreateMilestone("Admin workflows", new DateOnly(2026, 5, 15), "Add project editing and publish controls.")
                ]),
            CreateProject(
                title: "TransitPulse Dashboard",
                startDate: new DateOnly(2025, 8, 4),
                shortDescription: "A city transit operations dashboard for monitoring route health and service interruptions.",
                longDescriptionMarkdown: "TransitPulse aggregates schedule adherence, rider alerts, and maintenance status into a single dashboard for transit coordinators and dispatch teams.",
                githubUrl: "https://github.com/example/transitpulse",
                demoUrl: "https://demo.example.test/transitpulse",
                developerRoles: ["Backend Engineer", "Data Visualization"],
                technologies: ["ASP.NET Core", "D3.js", "Azure SQL"],
                skills: ["Observability", "Dashboard Design", "Data Modeling"],
                collaborators:
                [
                    CreateCollaborator("Riley Chen", "https://github.com/rileyc", null, "https://images.example.test/riley.png", ["Frontend Development"])
                ],
                milestones:
                [
                    CreateMilestone("Dispatch rollout", new DateOnly(2025, 11, 20), "Delivered the first dashboard pilot."),
                    CreateMilestone("Alert integrations", new DateOnly(2026, 1, 12), "Connected service alert feeds.")
                ]),
            CreateProject(
                title: "ClinicFlow Scheduling",
                startDate: new DateOnly(2024, 11, 1),
                shortDescription: "An appointment and room scheduling system for outpatient clinics with role-specific queues.",
                longDescriptionMarkdown: "ClinicFlow helps intake coordinators, providers, and support teams coordinate appointments, room usage, and same-day schedule changes with reduced administrative friction.",
                githubUrl: "https://github.com/example/clinicflow",
                demoUrl: null,
                developerRoles: ["Full Stack Engineer"],
                technologies: ["Blazor", ".NET", "SQL Server"],
                skills: ["Workflow Design", "Accessibility", "Domain Modeling"],
                collaborators:
                [
                    CreateCollaborator("Jordan Patel", null, null, null, ["Clinical Advisor"])
                ],
                milestones:
                [
                    CreateMilestone("Pilot clinic launch", new DateOnly(2025, 2, 18), "Launched with the first outpatient clinic.")
                ]),
            CreateProject(
                title: "SignalRoom Collaboration Hub",
                startDate: new DateOnly(2025, 3, 14),
                shortDescription: "A real-time project room for sharing updates, links, and meeting notes across distributed teams.",
                longDescriptionMarkdown: "SignalRoom combines lightweight chat, structured updates, and searchable project notes so distributed teams can reduce status-meeting overhead.",
                githubUrl: "https://github.com/example/signalroom",
                demoUrl: "https://demo.example.test/signalroom",
                developerRoles: ["Platform Engineer", "Realtime Systems"],
                technologies: ["SignalR", "React", "Redis"],
                skills: ["Realtime Messaging", "Collaboration UX", "Performance Tuning"],
                collaborators:
                [
                    CreateCollaborator("Casey Nguyen", "https://github.com/caseyn", "https://caseyn.dev", null, ["Frontend Development", "Interaction Design"])
                ],
                milestones:
                [
                    CreateMilestone("Realtime sync launch", new DateOnly(2025, 6, 30), "Introduced collaborative room updates.")
                ]),
            CreateProject(
                title: "LedgerLens Finance Tracker",
                startDate: new DateOnly(2024, 6, 9),
                shortDescription: "A personal finance tracker focused on categorized spending and simple forecasting visuals.",
                longDescriptionMarkdown: "LedgerLens helps users understand spending habits through fast categorization, monthly snapshots, and forward-looking budget projections.",
                githubUrl: "https://github.com/example/ledgerlens",
                demoUrl: "https://demo.example.test/ledgerlens",
                developerRoles: ["Product Engineer"],
                technologies: ["Vue", "Node.js", "PostgreSQL"],
                skills: ["Product Thinking", "Data Visualization", "UX Writing"],
                collaborators: [],
                milestones:
                [
                    CreateMilestone("Budget forecast release", new DateOnly(2024, 9, 15), "Released first forecasting tools.")
                ]),
            CreateProject(
                title: "VaultDocs Knowledge Base",
                startDate: new DateOnly(2025, 1, 27),
                shortDescription: "A secure internal knowledge base with structured publishing workflows and content ownership.",
                longDescriptionMarkdown: "VaultDocs supports governed documentation publishing with approval workflows, structured metadata, and searchable content collections.",
                githubUrl: "https://github.com/example/vaultdocs",
                demoUrl: null,
                developerRoles: ["Backend Engineer", "Information Architecture"],
                technologies: [".NET", "SQL Server", "Azure App Service"],
                skills: ["Content Systems", "Search", "Authorization"],
                collaborators:
                [
                    CreateCollaborator("Avery Ross", null, "https://averyross.dev", null, ["Information Architecture"])
                ],
                milestones:
                [
                    CreateMilestone("Publishing workflow", new DateOnly(2025, 4, 21), "Shipped draft and approval workflow.")
                ]),
            CreateProject(
                title: "CivicStory Archive",
                startDate: new DateOnly(2023, 9, 18),
                shortDescription: "A digital archive for preserving oral histories with tagging, transcript search, and media galleries.",
                longDescriptionMarkdown: "CivicStory gives local history organizations a searchable archive for interviews, transcripts, and related media with structured metadata.",
                githubUrl: "https://github.com/example/civicstory",
                demoUrl: "https://demo.example.test/civicstory",
                developerRoles: ["Lead Engineer", "Search Architecture"],
                technologies: ["Elasticsearch", "ASP.NET Core", "Azure Blob Storage"],
                skills: ["Search Relevance", "Metadata Modeling", "Media Management"],
                collaborators:
                [
                    CreateCollaborator("Parker Hill", "https://github.com/parkerhill", null, null, ["Archivist Liaison"])
                ],
                milestones:
                [
                    CreateMilestone("Transcript indexing", new DateOnly(2024, 1, 10), "Indexed transcripts for search.")
                ]),
            CreateProject(
                title: "OpsBeacon Incident Feed",
                startDate: new DateOnly(2025, 7, 8),
                shortDescription: "An internal incident feed for status updates, response tracking, and postmortem follow-through.",
                longDescriptionMarkdown: "OpsBeacon centralizes incident timelines, response notes, and action items so teams can keep communication aligned during and after outages.",
                githubUrl: "https://github.com/example/opsbeacon",
                demoUrl: "https://demo.example.test/opsbeacon",
                developerRoles: ["Site Reliability Engineer", "Backend Engineer"],
                technologies: ["React", ".NET", "SQL Server"],
                skills: ["Incident Response", "Status Communication", "Operational Tooling"],
                collaborators:
                [
                    CreateCollaborator("Devon Price", null, null, null, ["SRE Advisor"])
                ],
                milestones:
                [
                    CreateMilestone("Timeline composer", new DateOnly(2025, 8, 22), "Released structured incident timeline editing.")
                ]),
            CreateProject(
                title: "MentorMatch Platform",
                startDate: new DateOnly(2024, 2, 5),
                shortDescription: "A mentorship matching platform for pairing learners with mentors based on goals and skills.",
                longDescriptionMarkdown: "MentorMatch helps communities pair mentors and learners through interest mapping, availability, and structured progress check-ins.",
                githubUrl: "https://github.com/example/mentormatch",
                demoUrl: "https://demo.example.test/mentormatch",
                developerRoles: ["Full Stack Engineer", "Product Strategist"],
                technologies: ["React", "TypeScript", "Azure Functions"],
                skills: ["Matching Logic", "Forms UX", "Community Platforms"],
                collaborators:
                [
                    CreateCollaborator("Sam Rivera", "https://github.com/samrivera", "https://samrivera.dev", null, ["Community Research"])
                ],
                milestones:
                [
                    CreateMilestone("Pilot cohort onboarding", new DateOnly(2024, 5, 6), "Shipped first onboarding flow.")
                ]),
            CreateProject(
                title: "FieldNote Mobile",
                startDate: new DateOnly(2025, 10, 2),
                shortDescription: "A field-reporting app for capturing notes, photos, and location context while offline.",
                longDescriptionMarkdown: "FieldNote Mobile supports offline-first field reporting with synchronized uploads, structured observations, and photo attachments for distributed teams.",
                githubUrl: "https://github.com/example/fieldnote-mobile",
                demoUrl: null,
                developerRoles: ["Mobile Engineer", "API Designer"],
                technologies: [".NET MAUI", "SQLite", "ASP.NET Core"],
                skills: ["Offline Sync", "Mobile UX", "Resilient APIs"],
                collaborators:
                [
                    CreateCollaborator("Jamie Fox", null, "https://jamiefox.dev", "https://images.example.test/jamie.png", ["QA Lead"])
                ],
                milestones:
                [
                    CreateMilestone("Offline sync prototype", new DateOnly(2025, 12, 12), "Validated offline-first syncing.")
                ])
        };

        projects.AddRange(CreateGeneratedProjects());
        return projects;
    }

    private static IEnumerable<Project> CreateGeneratedProjects()
    {
        var tracks = new[]
        {
            new SeedTrack("Analytics", "Analytics Studio", ["C#", ".NET", "SQL Server"], ["Data Modeling", "Reporting", "Observability"]),
            new SeedTrack("Commerce", "Commerce Toolkit", ["React", "TypeScript", "Azure Functions"], ["UX Writing", "API Design", "Forms UX"]),
            new SeedTrack("Operations", "Operations Console", [".NET", "React", "Redis"], ["Operational Tooling", "Incident Response", "Performance Tuning"]),
            new SeedTrack("Education", "Learning Portal", ["Blazor", "SQL Server", "Azure App Service"], ["Accessibility", "Workflow Design", "Content Systems"]),
            new SeedTrack("Community", "Community Hub", ["React", "Node.js", "PostgreSQL"], ["Community Platforms", "Matching Logic", "Dashboard Design"])
        };

        for (var index = 1; index <= 90; index++)
        {
            var track = tracks[(index - 1) % tracks.Length];
            var year = 2023 + ((index - 1) % 4);
            var month = ((index - 1) % 12) + 1;
            var day = ((index - 1) % 25) + 1;
            var title = $"{track.Category} Sprint {index:00}";
            var projectNumber = index + 10;

            yield return CreateProject(
                title: title,
                startDate: new DateOnly(year, month, day),
                shortDescription: $"{track.Label} prototype #{projectNumber} focused on fast iteration and polished delivery.",
                longDescriptionMarkdown: $"{track.Label} prototype #{projectNumber} explores scalable workflows, cleaner reporting, and user-friendly interfaces for portfolio seed data and browsing scenarios.",
                githubUrl: $"https://github.com/example/{title.ToLowerInvariant().Replace(" ", "-")}",
                demoUrl: index % 4 == 0 ? null : $"https://demo.example.test/{title.ToLowerInvariant().Replace(" ", "-")}",
                developerRoles: ["Full Stack Engineer", index % 3 == 0 ? "Product Engineer" : "Backend Engineer"],
                technologies: track.Technologies,
                skills: track.Skills,
                collaborators:
                [
                    CreateCollaborator(
                        $"Seed Collaborator {projectNumber:000}",
                        index % 2 == 0 ? $"https://github.com/seed-collaborator-{projectNumber:000}" : null,
                        $"https://profiles.example.test/seed-collaborator-{projectNumber:000}",
                        null,
                        [index % 2 == 0 ? "QA Review" : "Design Review"])
                ],
                milestones:
                [
                    CreateMilestone("Prototype kickoff", new DateOnly(year, month, Math.Min(day, 20)), $"Started scoped discovery for generated project {projectNumber:000}."),
                    CreateMilestone("Iteration review", new DateOnly(year, month, Math.Min(day + 5, 28)), $"Captured iteration notes for generated project {projectNumber:000}.")
                ]);
        }
    }

    private static Project CreateProject(
        string title,
        DateOnly startDate,
        string shortDescription,
        string longDescriptionMarkdown,
        string? githubUrl,
        string? demoUrl,
        IEnumerable<string> developerRoles,
        IEnumerable<string> technologies,
        IEnumerable<string> skills,
        IEnumerable<ProjectCollaborator> collaborators,
        IEnumerable<ProjectMilestone> milestones)
    {
        var slug = title.ToLowerInvariant().Replace(" ", "-");

        return new Project
        {
            Title = title,
            StartDate = startDate,
            PrimaryImageUrl = $"https://images.example.test/projects/{slug}/hero.png",
            ShortDescription = shortDescription,
            LongDescriptionMarkdown = longDescriptionMarkdown,
            GitHubUrl = githubUrl,
            DemoUrl = demoUrl,
            IsPublished = true,
            IsFeatured = title is "Project Portfolio 2026" or "TransitPulse Dashboard" or "SignalRoom Collaboration Hub" or "CivicStory Archive" or "MentorMatch Platform",
            DeveloperRoles = developerRoles.Select(role => new ProjectDeveloperRole { Name = role }).ToList(),
            Technologies = technologies.Select(technology => new ProjectTechnology { Name = technology }).ToList(),
            Skills = skills.Select(skill => new ProjectSkill { Name = skill }).ToList(),
            Collaborators = collaborators.ToList(),
            Milestones = milestones.ToList(),
            Screenshots = CreateScreenshots(title, slug)
        };
    }

    private static List<ProjectScreenshot> CreateScreenshots(string title, string slug)
    {
        if (title == "Project Portfolio 2026")
        {
            return
            [
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-1.png",
                    Caption = "Homepage hero and featured project carousel",
                    SortOrder = 1
                },
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-2.png",
                    Caption = "Project list search and filtering experience",
                    SortOrder = 2
                },
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-3.png",
                    Caption = "Project detail overview with screenshot rail",
                    SortOrder = 3
                },
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-4.png",
                    Caption = "Fullscreen screenshot viewer",
                    SortOrder = 4
                },
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-5.png",
                    Caption = "Responsive mobile detail layout",
                    SortOrder = 5
                },
                new ProjectScreenshot
                {
                    ImageUrl = $"https://images.example.test/projects/{slug}/screen-6.png",
                    Caption = "Dark theme audit across public pages",
                    SortOrder = 6
                }
            ];
        }

        return
        [
            new ProjectScreenshot
            {
                ImageUrl = $"https://images.example.test/projects/{slug}/screen-1.png",
                Caption = "Primary workflow",
                SortOrder = 1
            },
            new ProjectScreenshot
            {
                ImageUrl = $"https://images.example.test/projects/{slug}/screen-2.png",
                Caption = "Detail view",
                SortOrder = 2
            }
        ];
    }

    private static ProjectCollaborator CreateCollaborator(
        string name,
        string? githubProfileUrl,
        string? websiteUrl,
        string? photoUrl,
        IEnumerable<string> roles)
    {
        return new ProjectCollaborator
        {
            Name = name,
            GitHubProfileUrl = githubProfileUrl,
            WebsiteUrl = websiteUrl,
            PhotoUrl = photoUrl,
            Roles = roles.Select(role => new ProjectCollaboratorRole { Name = role }).ToList()
        };
    }

    private static ProjectMilestone CreateMilestone(string title, DateOnly targetDate, string description)
    {
        return new ProjectMilestone
        {
            Title = title,
            TargetDate = targetDate,
            Description = description
        };
    }

    private sealed record SeedTrack(
        string Category,
        string Label,
        IReadOnlyList<string> Technologies,
        IReadOnlyList<string> Skills);
}
