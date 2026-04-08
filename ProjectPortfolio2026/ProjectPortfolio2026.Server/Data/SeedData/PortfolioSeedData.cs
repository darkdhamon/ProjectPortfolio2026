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
            DisplayName = "Bronze Loft",
            ContactHeadline = "Choose the contact path that fits the conversation you want to have.",
            ContactIntro = "This portfolio frames outreach as a calm next step. Recruiters, collaborators, and hiring teams should be able to find the right channel quickly without sorting through hardcoded one-off content.",
            AvailabilityHeadline = "Open to new opportunities",
            AvailabilitySummary = "Focused on full-stack product engineering roles where API design, thoughtful UI, and maintainable delivery all matter.",
            IsPublic = true,
            ContactMethods =
            [
                new PortfolioContactMethod
                {
                    Type = "email",
                    Label = "Email",
                    Value = "bronze@example.dev",
                    Note = "Best for interview requests, consulting inquiries, and longer-form conversations.",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "phone",
                    Label = "Phone",
                    Value = "(312) 555-0147",
                    Note = "Available for scheduled calls on weekdays between 9 AM and 5 PM Central.",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "location",
                    Label = "Location",
                    Value = "Chicago, Illinois",
                    Note = "Open to remote roles, hybrid collaboration, and select on-site visits.",
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
                    Summary = "Code samples, ongoing portfolio work, and implementation details.",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Platform = "linkedin",
                    Label = "LinkedIn",
                    Url = "https://www.linkedin.com/in/bronze-loft",
                    Handle = "Bronze Loft",
                    Summary = "Professional background, role history, and recruiter-friendly context.",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Platform = "calendly",
                    Label = "Calendly",
                    Url = "https://calendly.com/bronze-loft/portfolio-intro",
                    Handle = "Schedule an intro",
                    Summary = "A lightweight path for a first conversation without email back-and-forth.",
                    SortOrder = 3,
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
                Name = "Northwind Health",
                City = "Chicago",
                Region = "IL",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Senior Software Engineer",
                        new DateOnly(2024, 1, 8),
                        null,
                        "Dana Smith",
                        """
                        Leading API delivery, platform refactoring, and public-facing portfolio architecture work.

                        Partnering with product and design stakeholders to align engineering implementation with recruiting and resume-generation goals.
                        """,
                        skills: ["API Design", "Technical Writing"],
                        technologies: [".NET 10", "SQL Server"]),
                    CreateJobRole(
                        "Software Engineer",
                        new DateOnly(2022, 4, 4),
                        new DateOnly(2023, 12, 29),
                        "Dana Smith",
                        """
                        Delivered internal business applications and improved deployment reliability for line-of-business systems.
                        """,
                        skills: ["Workflow Design", "Testing"],
                        technologies: ["ASP.NET Core", "Azure DevOps"])
                ]
            },
            new Employer
            {
                Name = "Blue Ocean Labs",
                City = "Austin",
                Region = "TX",
                Country = "USA",
                IsPublished = true,
                JobRoles =
                [
                    CreateJobRole(
                        "Platform Engineer",
                        new DateOnly(2020, 6, 1),
                        new DateOnly(2022, 3, 18),
                        "Morgan Patel",
                        """
                        Built shared backend components and supported product teams with data access and deployment tooling improvements.
                        """,
                        skills: ["Data Modeling", "Performance Tuning"],
                        technologies: ["C#", "SQL Server"])
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
