export interface NavItem {
    label: string;
    description: string;
    href?: string;
}

export const navItems: readonly NavItem[] = [
    { label: 'Home', href: '/', description: 'Featured highlights and introduction.' },
    { label: 'Projects', href: '/projects', description: 'Browse shipped work and project detail stories.' },
    { label: 'Admin', href: '/admin', description: 'Manage access, account settings, and admin tools.' },
    { label: 'Timeline', description: 'Career milestones, education, and certifications.' },
    { label: 'About', description: 'Background, strengths, and developer story.' },
    { label: 'Resume', description: 'Resume hub and downloadable materials.' },
    { label: 'Contact', description: 'Direct outreach paths and social links.' },
    { label: 'Blog', description: 'Writing, updates, and thought pieces.' }
] as const;
