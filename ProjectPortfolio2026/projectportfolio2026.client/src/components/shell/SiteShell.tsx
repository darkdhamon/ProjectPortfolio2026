import type { ReactNode } from 'react';
import portfolioLogoIcon from '../../assets/Logo/Portfolio-Logo-Icon.png';
import type { NavigateFn } from '../../app/navigation';
import { InternalLink } from '../common/InternalLink';

export interface SiteShellContent {
    kicker: string;
    title: string;
    summary: string;
}

interface NavItem {
    label: string;
    description: string;
    href?: string;
}

const navItems: readonly NavItem[] = [
    { label: 'Home', href: '/', description: 'Featured highlights and introduction.' },
    { label: 'Projects', href: '/projects', description: 'Browse shipped work and project detail stories.' },
    { label: 'Timeline', description: 'Career milestones, education, and certifications.' },
    { label: 'About', description: 'Background, strengths, and developer story.' },
    { label: 'Resume', description: 'Resume hub and downloadable materials.' },
    { label: 'Contact', description: 'Direct outreach paths and social links.' },
    { label: 'Blog', description: 'Writing, updates, and thought pieces.' }
] as const;

interface SiteShellProps {
    activeNavLabel: string;
    content: SiteShellContent;
    onNavigate: NavigateFn;
    children: ReactNode;
}

export function SiteShell({
    activeNavLabel,
    content,
    onNavigate,
    children
}: SiteShellProps) {
    return (
        <div className="site-shell">
            <aside className="site-sidebar" aria-label="Primary">
                <div className="site-brand">
                    <div className="brand-mark" aria-hidden="true">
                        <img src={portfolioLogoIcon} alt="" />
                    </div>
                    <div>
                        <strong>
                            <span>Project</span>
                            <span>Portfolio</span>
                        </strong>
                    </div>
                </div>

                <nav className="site-nav" aria-label="Primary site navigation">
                    {navItems.map(item => item.href ? (
                        <InternalLink
                            key={item.label}
                            className={`nav-link${activeNavLabel === item.label ? ' active' : ''}`}
                            href={item.href}
                            ariaCurrent={activeNavLabel === item.label ? 'page' : undefined}
                            onNavigate={onNavigate}>
                            {item.label}
                        </InternalLink>
                    ) : (
                        <button
                            key={item.label}
                            className="nav-link nav-link-disabled"
                            type="button"
                            disabled
                            aria-disabled="true">
                            <span>{item.label}</span>
                            <span className="coming-soon-pill">Coming Soon</span>
                        </button>
                    ))}
                </nav>

                <p className="sidebar-footnote">
                    Resume will eventually include the Work History subsection.
                </p>
            </aside>

            <div className="site-main">
                <header className="site-header">
                    <div>
                        <p className="header-kicker">{content.kicker}</p>
                        <p className="site-title">{content.title}</p>
                    </div>
                    <p className="site-header-copy">{content.summary}</p>
                </header>

                {children}
            </div>
        </div>
    );
}
