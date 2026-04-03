export interface ProjectSummary {
    requestId?: string;
    id: number;
    title: string;
    startDate: string;
    endDate?: string | null;
    primaryImageUrl?: string | null;
    shortDescription: string;
    gitHubUrl?: string | null;
    demoUrl?: string | null;
    isFeatured: boolean;
    skills: string[];
    technologies: string[];
}

export interface ProjectScreenshot {
    imageUrl: string;
    caption?: string | null;
    sortOrder: number;
}

export interface ProjectCollaborator {
    name: string;
    gitHubProfileUrl?: string | null;
    websiteUrl?: string | null;
    photoUrl?: string | null;
    roles: string[];
}

export interface ProjectMilestone {
    title: string;
    targetDate: string;
    completedOn?: string | null;
    description?: string | null;
}

export interface ProjectDetail {
    requestId?: string;
    id: number;
    title: string;
    startDate: string;
    endDate?: string | null;
    primaryImageUrl?: string | null;
    shortDescription: string;
    longDescriptionMarkdown: string;
    gitHubUrl?: string | null;
    demoUrl?: string | null;
    isPublished: boolean;
    isFeatured: boolean;
    screenshots: ProjectScreenshot[];
    developerRoles: string[];
    technologies: string[];
    skills: string[];
    collaborators: ProjectCollaborator[];
    milestones: ProjectMilestone[];
}

export interface ProjectListResponse {
    requestId?: string;
    items: ProjectSummary[];
    page: number;
    pageSize: number;
    totalCount: number;
    hasMore: boolean;
    availableSkills: string[];
}

export interface FeaturedProjectsResponse {
    requestId?: string;
    items: ProjectSummary[];
}

export interface ApiErrorResponse {
    message?: string;
}
