using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Data.SeedData;

public static class PortfolioSeedData
{
    public static async Task InitializeAsync(PortfolioDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var hasProjects = await dbContext.Projects.AnyAsync(cancellationToken);
        var hasPortfolioProfile = await dbContext.PortfolioProfiles.AnyAsync(cancellationToken);
        var hasEmployers = await dbContext.Employers.AnyAsync(cancellationToken);
        List<Project> seededProjects = [];

        if (!hasPortfolioProfile)
        {
            dbContext.PortfolioProfiles.Add(CreatePortfolioProfile());
        }

        if (!hasProjects)
        {
            seededProjects = CreateProjects();
            NormalizeProjectTags(seededProjects, []);
            dbContext.Projects.AddRange(seededProjects);
        }

        if (!hasEmployers)
        {
            var employers = CreateEmployers();
            var tagSourceProjects = seededProjects.Count > 0
                ? seededProjects
                : await dbContext.Projects
                    .Include(project => project.ProjectTags)
                        .ThenInclude(projectTag => projectTag.Tag)
                    .ToListAsync(cancellationToken);

            NormalizeEmployerTags(employers, tagSourceProjects);
            dbContext.Employers.AddRange(employers);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PortfolioProfile CreatePortfolioProfile()
    {
        return new PortfolioProfile
        {
            DisplayName = "Bronze Harold Brown",
            ContactHeadline = "Senior .NET Cloud & AI-Integrated Engineer",
            ContactIntro = "Senior .NET engineer with 12+ years of experience designing, building, testing, deploying, and supporting software solutions across healthcare, insurance, finance, and music technology. Strongest depth is in backend C#/.NET development, API design, data integration, debugging, production troubleshooting, and SQL-backed business systems, with growing hands-on React experience through current project work.",
            AvailabilityHeadline = "Open to senior .NET, full-stack, cloud, and AI-integrated engineering roles.",
            AvailabilitySummary = "Best fit is backend-heavy C#/.NET work with room to contribute across the stack, deepen frontend experience, support architecture and mentorship, and use AI tools responsibly as part of practical software delivery.",
            IsPublic = true,
            ContactMethods =
            [
                new PortfolioContactMethod
                {
                    Type = "email",
                    Label = "Email",
                    Value = "Bronze.H.Brown@gmail.com",
                    Note = "Best for interview requests, consulting inquiries, and recruiter conversations.",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "phone",
                    Label = "Phone",
                    Value = "(240) 758-6723",
                    Note = "Available for scheduled calls and follow-up conversations.",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "location",
                    Label = "Location",
                    Value = "Madison Lake, Minnesota",
                    Note = "Open to remote roles and broader engineering collaboration.",
                    SortOrder = 3,
                    IsVisible = true
                }
            ],
            SocialLinks =
            [
                new PortfolioSocialLink
                {
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    Handle = "@darkdhamon",
                    Summary = "Public source code, current portfolio work, and implementation details.",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Platform = "linkedin",
                    Label = "LinkedIn",
                    Url = "https://www.linkedin.com/in/bronzeharoldbrown/",
                    Handle = "Bronze Harold Brown",
                    Summary = "Professional background, role history, and recruiter-facing context.",
                    SortOrder = 2,
                    IsVisible = true
                }
            ]
        };
    }

    private static List<Project> CreateProjects()
    {
        var projects = new List<Project>
        {
            CreateProject(
                title: "Project Portfolio 2026",
                startDate: new DateOnly(2026, 1, 10),
                endDate: null,
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
                endDate: null,
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
                endDate: new DateOnly(2025, 3, 28),
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
                endDate: new DateOnly(2025, 9, 18),
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
                endDate: new DateOnly(2024, 11, 7),
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
                endDate: new DateOnly(2025, 5, 30),
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
                endDate: new DateOnly(2024, 2, 14),
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
                endDate: new DateOnly(2025, 10, 24),
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
                endDate: new DateOnly(2024, 7, 19),
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
                endDate: null,
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
            var year = 2015 + ((index - 1) % 12);
            var month = ((index - 1) % 12) + 1;
            var day = ((index - 1) % 25) + 1;
            var title = $"{track.Category} Sprint {index:00}";
            var projectNumber = index + 10;
            var startDate = new DateOnly(year, month, day);
            var durationMonths = 2 + ((index - 1) % 9);
            var rawEndDate = startDate.AddMonths(durationMonths).AddDays(((index - 1) % 12) + 2);
            var endDate = rawEndDate.Year > 2026
                ? new DateOnly(2026, 12, 31)
                : rawEndDate;

            yield return CreateProject(
                title: title,
                startDate: startDate,
                endDate: endDate,
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
        DateOnly? endDate,
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
            EndDate = endDate,
            PrimaryImageUrl = $"https://images.example.test/projects/{slug}/hero.png",
            ShortDescription = shortDescription,
            LongDescriptionMarkdown = longDescriptionMarkdown,
            GitHubUrl = githubUrl,
            DemoUrl = demoUrl,
            IsPublished = true,
            IsFeatured = title is "Project Portfolio 2026" or "TransitPulse Dashboard" or "SignalRoom Collaboration Hub" or "CivicStory Archive" or "MentorMatch Platform",
            DeveloperRoles = developerRoles.Select(role => new ProjectDeveloperRole { Name = role }).ToList(),
            ProjectTags = CreateProjectTags(TagCategory.Technology, technologies)
                .Concat(CreateProjectTags(TagCategory.Skill, skills))
                .ToList(),
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

    private static List<Employer> CreateEmployers()
    {
        return
        [
            new Employer
            {
                Name = "Axl Protocol Music",
                City = "Madison Lake",
                Region = "MN",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        ".NET Full-Stack Cloud Engineer",
                        new DateOnly(2024, 10, 1),
                        null,
                        null,
                        """
                        Led the architecture and delivery of a .NET 10 Blazor web application with interactive server rendering, backed by MongoDB and deployed to Azure App Service.

                        Owned the full software development lifecycle from concept and requirements through implementation, testing, deployment, support, CI quality gates, analytics, and OpenAI-assisted platform features under my technical direction.
                        """,
                        skills: ["Application Architecture", "AI Integration", "CI/CD", "Content Management", "Analytics"],
                        technologies: [".NET 10", "Blazor", "ASP.NET Core", "MongoDB", "Azure App Service", "GitHub Actions", "OpenAI Responses API"])
                ]
            },
            new Employer
            {
                Name = "Tata Consultancy Services (TCS)",
                City = null,
                Region = null,
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Engineering Consultant",
                        new DateOnly(2024, 3, 1),
                        new DateOnly(2025, 8, 31),
                        null,
                        """
                        Contributed to a new ASP.NET MVC healthcare application for Humana Military, building MVC and Razor forms, supporting business logic, and collaborating with analysts and product owners.

                        Supported production operations, issue triage, documentation review, escalation workflows, and Agile team delivery across multiple client engagements.
                        """,
                        skills: ["MVC Development", "Production Support", "Agile Delivery"],
                        technologies: ["C#", "ASP.NET MVC", "Razor", "Web API", "Azure DevOps"])
                ]
            },
            new Employer
            {
                Name = "TEKsystems",
                City = null,
                Region = null,
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Engineering Contractor (ORAU)",
                        new DateOnly(2023, 5, 1),
                        new DateOnly(2023, 8, 31),
                        null,
                        """
                        Engineered new application features using .NET 7, C#, and ASP.NET MVC while collaborating in an Agile and Azure DevOps workflow.

                        Debugged complex issues, reviewed CI/CD logs, and supported defect isolation across development and QA environments.
                        """,
                        skills: ["Debugging", "Agile Delivery", "CI/CD"],
                        technologies: ["C#", ".NET 7", "ASP.NET MVC", "T-SQL", "Azure DevOps"]),
                    CreateJobRole(
                        "Senior Software Engineer Contractor (Availity)",
                        new DateOnly(2022, 11, 1),
                        new DateOnly(2023, 2, 28),
                        null,
                        """
                        Led debugging and feature work for insurance-related ASP.NET MVC applications while partnering with analysts around EDI-aligned business requirements.

                        Authored unit tests and delivered .NET 6 changes within an Azure DevOps and Agile delivery model.
                        """,
                        skills: ["Unit Testing", "EDI Domain Support", "Debugging"],
                        technologies: ["C#", ".NET 6", "ASP.NET MVC", "Azure DevOps", "PL/SQL"])
                ]
            },
            new Employer
            {
                Name = "Robert Half",
                City = "Pittsburgh",
                Region = "PA",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Senior Software Engineer Consultant",
                        new DateOnly(2021, 3, 1),
                        new DateOnly(2022, 10, 31),
                        null,
                        """
                        Developed and deployed RESTful APIs and ASP.NET MVC application features across multiple consulting engagements while adapting quickly to varied domains and client needs.

                        Helped clients with planning, estimates, code reviews, and end-to-end software delivery using C#, .NET Core, Entity Framework, and Azure DevOps.
                        """,
                        skills: ["REST API Design", "Consulting", "Project Planning"],
                        technologies: ["C#", ".NET Core", "ASP.NET MVC", "ASP.NET Web API", "Entity Framework Core", "Azure DevOps"])
                ]
            },
            new Employer
            {
                Name = "Delta Care RX",
                City = "Pittsburgh",
                Region = "PA",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Engineer",
                        new DateOnly(2019, 9, 1),
                        new DateOnly(2021, 1, 31),
                        null,
                        """
                        Led architecture and development of a Vue.js patient identity-confirmation application and implemented secure REST APIs with JWT authentication.

                        Managed MySQL data access, supported PDF-based document workflows, and improved reliability through debugging, testing, and custom JavaScript maintenance.
                        """,
                        skills: ["REST API Design", "JWT Authentication", "Frontend Development"],
                        technologies: ["C#", "ASP.NET Core", "ASP.NET Web API", "Vue.js", "MySQL", "Entity Framework Core"])
                ]
            },
            new Employer
            {
                Name = "Sentara Health Care",
                City = "Virginia Beach",
                Region = "VA",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Engineer",
                        new DateOnly(2018, 3, 1),
                        new DateOnly(2019, 9, 30),
                        null,
                        """
                        Designed RESTful APIs and middleware integrations, including Epic InterConnect transformation work and ETL processes for CSV and FTP-delivered healthcare data.

                        Supported Swagger and AutoRest-based client generation, SQL Server data access, Jenkins and Azure DevOps delivery workflows, and 24/7 on-call production support.
                        """,
                        skills: ["ETL", "API Integration", "On-Call Support"],
                        technologies: ["C#", "ASP.NET Core", "SQL Server", "Azure", "Swagger", "AutoRest", "Jenkins"])
                ]
            },
            new Employer
            {
                Name = "IMPAQ International",
                City = "Columbia",
                Region = "MD",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Developer",
                        new DateOnly(2017, 5, 1),
                        new DateOnly(2018, 2, 28),
                        null,
                        """
                        Maintained and enhanced ASP.NET applications, implemented new features, and authored unit tests to improve application robustness.

                        Supported performance tuning, Jenkins-based release workflows, and Entity Framework-backed relational data access in a Team Foundation Server environment.
                        """,
                        skills: ["Application Maintenance", "Unit Testing", "Performance Tuning"],
                        technologies: ["C#", "ASP.NET MVC", "Entity Framework", "SQL", "Jenkins"])
                ]
            },
            new Employer
            {
                Name = "FEi Systems",
                City = "Columbia",
                Region = "MD",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Contract Developer",
                        new DateOnly(2016, 9, 1),
                        new DateOnly(2017, 2, 28),
                        null,
                        """
                        Built and maintained PDF generation templates, contributed CSS and JavaScript UI improvements, and resolved software defects in established ASP.NET MVC applications.
                        """,
                        skills: ["PDF Generation", "UI Debugging", "Rapid Learning"],
                        technologies: ["C#", "ASP.NET MVC", "CSS", "JavaScript", "RavenDB", "ABCpdf"])
                ]
            },
            new Employer
            {
                Name = "Revature",
                City = "Reston",
                Region = "VA",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Contract Developer",
                        new DateOnly(2016, 2, 1),
                        new DateOnly(2016, 8, 31),
                        null,
                        """
                        Completed an intensive training program focused on emerging technologies and delivered projects using SQL Server, C#, .NET, ASP.NET MVC, and AngularJS within Agile teams.
                        """,
                        skills: ["Training", "Agile Delivery", "Form Development"],
                        technologies: ["C#", "ASP.NET MVC", "ASP.NET Web Forms", "Entity Framework", "SQL Server", "AngularJS"])
                ]
            },
            new Employer
            {
                Name = "MCS Valuations",
                City = "Sandy",
                Region = "UT",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Developer",
                        new DateOnly(2015, 1, 1),
                        new DateOnly(2015, 10, 31),
                        null,
                        """
                        Maintained and upgraded the primary valuation application, implemented new SQL stored procedures, and supported rebranding-focused UI updates.

                        Applied debugging and modernization work across legacy and newer Microsoft-stack technologies.
                        """,
                        skills: ["Debugging", "Stored Procedures", "UI Rebranding"],
                        technologies: ["C#", "VB.NET", "VB6", "SQL Server", "Classic ASP"])
                ]
            },
            new Employer
            {
                Name = "Zycamore LLC",
                City = "Provo",
                Region = "UT",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Software Developer",
                        new DateOnly(2014, 4, 1),
                        new DateOnly(2014, 12, 31),
                        null,
                        """
                        Built SQL generation scripts, C# file-generation utilities, and ASP.NET MVC form validation features while also supporting Windows Server and IIS setup work.
                        """,
                        skills: ["Server Provisioning", "Form Validation", "Automation"],
                        technologies: ["C#", "ASP.NET MVC", "SQL Server", "IIS", "Windows Server"])
                ]
            },
            new Employer
            {
                Name = "Ocean Avenue",
                City = "South Jordan",
                Region = "UT",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Junior Software Developer",
                        new DateOnly(2013, 3, 1),
                        new DateOnly(2013, 11, 30),
                        null,
                        """
                        Published code to development and production environments for a Sitefinity-based ASP.NET site, implemented backend C# business logic, and enhanced client-side behavior with JavaScript and jQuery.

                        Added AJAX features, SQL reporting support, and integrations with third-party OData web services.
                        """,
                        skills: ["Release Support", "OData Integration", "AJAX"],
                        technologies: ["C#", "ASP.NET", "Sitefinity", "JavaScript", "jQuery", "SQL Server"])
                ]
            },
            new Employer
            {
                Name = "Rubio's",
                City = null,
                Region = null,
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Intern Developer",
                        new DateOnly(2012, 10, 1),
                        new DateOnly(2012, 12, 31),
                        null,
                        """
                        Supported an enterprise reporting application, implemented C# business rules in an ASP.NET application, and participated in daily Scrum collaboration.
                        """,
                        skills: ["Business Rules", "Enterprise Reporting", "Scrum"],
                        technologies: ["C#", "ASP.NET", "Team Foundation Server"])
                ]
            },
            new Employer
            {
                Name = "TopVue Defense",
                City = null,
                Region = null,
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Intern Developer",
                        new DateOnly(2012, 10, 1),
                        new DateOnly(2012, 12, 31),
                        null,
                        """
                        Developed enterprise application features with PL/SQL, ASP.NET, C#, and JavaScript while collaborating through Team Foundation Server and daily Scrum meetings.
                        """,
                        skills: ["PL/SQL Development", "Enterprise Collaboration", "Scrum"],
                        technologies: ["PL/SQL", "C#", "ASP.NET", "JavaScript"])
                ]
            },
            new Employer
            {
                Name = "GTECH",
                City = null,
                Region = null,
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Intern Developer",
                        new DateOnly(2012, 6, 1),
                        new DateOnly(2012, 9, 30),
                        null,
                        """
                        Supported a lottery-simulation enterprise project by generating automated test cases and building Java utilities for XML parsing and file I/O workflows.
                        """,
                        skills: ["Automated Testing", "XML Parsing", "File I/O"],
                        technologies: ["Java", "XML", "Eclipse"])
                ]
            }
        ];
    }

    private static IEnumerable<ProjectTag> CreateProjectTags(TagCategory category, IEnumerable<string> values)
    {
        return values.Select(value => new ProjectTag
        {
            Tag = new Tag
            {
                Category = category,
                DisplayName = value,
                NormalizedName = NormalizeTagName(value)
            }
        });
    }

    private static void NormalizeProjectTags(IEnumerable<Project> projects, IEnumerable<Employer> employers)
    {
        var sharedTags = CreateSharedTagLookup(projects, employers);

        foreach (var project in projects)
        {
            project.ProjectTags = project.ProjectTags
                .Select(projectTag =>
                {
                    var sourceTag = projectTag.Tag ?? throw new InvalidOperationException("Seed tags must include tag metadata.");
                    var key = (sourceTag.Category, sourceTag.NormalizedName);

                    if (!sharedTags.TryGetValue(key, out var sharedTag))
                    {
                        sharedTag = new Tag
                        {
                            Category = sourceTag.Category,
                            DisplayName = sourceTag.DisplayName,
                            NormalizedName = sourceTag.NormalizedName
                        };

                        sharedTags[key] = sharedTag;
                    }

                    return new ProjectTag
                    {
                        Tag = sharedTag
                    };
                })
                .ToList();
        }
    }

    private static void NormalizeEmployerTags(IEnumerable<Employer> employers, IEnumerable<Project> projects)
    {
        var sharedTags = CreateSharedTagLookup(projects, employers);

        foreach (var employer in employers)
        {
            foreach (var jobRole in employer.JobRoles)
            {
                jobRole.JobRoleTags = jobRole.JobRoleTags
                    .Select(jobRoleTag =>
                    {
                        var sourceTag = jobRoleTag.Tag ?? throw new InvalidOperationException("Seed tags must include tag metadata.");
                        var key = (sourceTag.Category, sourceTag.NormalizedName);

                        if (!sharedTags.TryGetValue(key, out var sharedTag))
                        {
                            sharedTag = new Tag
                            {
                                Category = sourceTag.Category,
                                DisplayName = sourceTag.DisplayName,
                                NormalizedName = sourceTag.NormalizedName
                            };

                            sharedTags[key] = sharedTag;
                        }

                        return new JobRoleTag
                        {
                            Tag = sharedTag
                        };
                    })
                    .ToList();
            }
        }
    }

    private static Dictionary<(TagCategory Category, string NormalizedName), Tag> CreateSharedTagLookup(
        IEnumerable<Project> projects,
        IEnumerable<Employer> employers)
    {
        var sharedTags = new Dictionary<(TagCategory Category, string NormalizedName), Tag>();

        foreach (var project in projects)
        {
            foreach (var projectTag in project.ProjectTags)
            {
                var sourceTag = projectTag.Tag;
                if (sourceTag is null)
                {
                    continue;
                }

                sharedTags.TryAdd((sourceTag.Category, sourceTag.NormalizedName), sourceTag);
            }
        }

        foreach (var employer in employers)
        {
            foreach (var jobRoleTag in employer.JobRoles.SelectMany(jobRole => jobRole.JobRoleTags))
            {
                var sourceTag = jobRoleTag.Tag;
                if (sourceTag is null)
                {
                    continue;
                }

                sharedTags.TryAdd((sourceTag.Category, sourceTag.NormalizedName), sourceTag);
            }
        }

        return sharedTags;
    }

    private static string NormalizeTagName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static JobRole CreateJobRole(
        string role,
        DateOnly startDate,
        DateOnly? endDate,
        string? supervisorName,
        string descriptionMarkdown,
        IEnumerable<string> skills,
        IEnumerable<string> technologies)
    {
        return new JobRole
        {
            Role = role,
            StartDate = startDate,
            EndDate = endDate,
            SupervisorName = supervisorName,
            DescriptionMarkdown = descriptionMarkdown,
            JobRoleTags = CreateJobRoleTags(TagCategory.Skill, skills)
                .Concat(CreateJobRoleTags(TagCategory.Technology, technologies))
                .ToList()
        };
    }

    private static IEnumerable<JobRoleTag> CreateJobRoleTags(TagCategory category, IEnumerable<string> values)
    {
        return values.Select(value => new JobRoleTag
        {
            Tag = new Tag
            {
                Category = category,
                DisplayName = value,
                NormalizedName = NormalizeTagName(value)
            }
        });
    }
}
