export type AdminSectionId = 'dashboard' | 'projects' | 'social-links' | 'resume' | 'publishing';

export interface AdminSectionDefinition {
    id: AdminSectionId;
    href: string;
    kicker: string;
    title: string;
    summary: string;
    issueReferences: string;
}

export const adminSections: readonly AdminSectionDefinition[] = [
    {
        id: 'dashboard',
        href: '/admin',
        kicker: 'Admin Workspace',
        title: 'Content Management Overview',
        summary: 'Use the admin shell to move between the planned management areas without rewriting routes or navigation later.',
        issueReferences: '#64'
    },
    {
        id: 'projects',
        href: '/admin/projects',
        kicker: 'Admin Workspace',
        title: 'Project Management',
        summary: 'Create, edit, archive, and feature public projects from a dedicated workflow instead of mixing those actions into one oversized page.',
        issueReferences: '#65, #23, #103'
    },
    {
        id: 'social-links',
        href: '/admin/social-links',
        kicker: 'Admin Workspace',
        title: 'Social Links',
        summary: 'Keep public profile links and outreach destinations in one place so contact and portfolio surfaces stay aligned.',
        issueReferences: '#66'
    },
    {
        id: 'resume',
        href: '/admin/resume',
        kicker: 'Admin Workspace',
        title: 'Resume Configuration',
        summary: 'Centralize the structured resume settings that feed the public resume experience and later import workflows.',
        issueReferences: '#67, #61'
    },
    {
        id: 'publishing',
        href: '/admin/publishing',
        kicker: 'Admin Workspace',
        title: 'Publishing And Preview',
        summary: 'Reserve a stable area for draft states, preview controls, and future publish workflows before those features go live.',
        issueReferences: '#68'
    }
] as const;

export function getAdminSection(sectionId: AdminSectionId) {
    return adminSections.find(section => section.id === sectionId) ?? adminSections[0];
}
