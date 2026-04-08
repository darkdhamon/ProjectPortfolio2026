import { type ReactNode } from 'react';
import portfolioLogoIcon from '../../assets/Logo/Portfolio-Logo-Icon.png';
import { InternalLink, type NavigateHandler } from '../navigation/InternalLink';
import { navItems } from './siteNavigation';

export interface SiteShellContent {
    kicker: string;
    title: string;
    summary: string;
}

export function SiteShell({
    activeNavLabel,
    content,
    currentUserDisplayName,
    isAuthenticated,
    onAdminNavigate,
    onLogout,
    onNavigate,
    children
}: {
    activeNavLabel: string;
    content: SiteShellContent;
    currentUserDisplayName: string;
    isAuthenticated: boolean;
    onAdminNavigate: () => void;
    onLogout: () => void;
    onNavigate: NavigateHandler;
    children: ReactNode;
}) {
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
                    {navItems.map(item => {
                        if (item.label === 'Admin') {
                            return (
                                <div key={item.label} className={`admin-nav-group${activeNavLabel === item.label ? ' expanded' : ''}`}>
                                    <button
                                        className={`nav-link nav-link-button${activeNavLabel === item.label ? ' active' : ''}`}
                                        type="button"
                                        aria-current={activeNavLabel === item.label ? 'page' : undefined}
                                        onClick={onAdminNavigate}>
                                        <span>{item.label}</span>
                                        <span className="coming-soon-pill admin-pill">{isAuthenticated ? 'Live' : 'Secure'}</span>
                                    </button>

                                    {isAuthenticated ? (
                                        <div className="admin-subnav" aria-label="Admin navigation">
                                            <InternalLink
                                                className={`subnav-link${content.title === 'Admin Dashboard' ? ' active' : ''}`}
                                                href="/admin"
                                                ariaCurrent={content.title === 'Admin Dashboard' ? 'page' : undefined}
                                                onNavigate={onNavigate}>
                                                Dashboard
                                            </InternalLink>
                                            <InternalLink
                                                className={`subnav-link${content.title === 'Account Settings' ? ' active' : ''}`}
                                                href="/admin/account"
                                                ariaCurrent={content.title === 'Account Settings' ? 'page' : undefined}
                                                onNavigate={onNavigate}>
                                                Account Settings
                                            </InternalLink>
                                            <button
                                                className="subnav-link subnav-button"
                                                type="button"
                                                onClick={onLogout}>
                                                Log out
                                            </button>
                                        </div>
                                    ) : null}
                                </div>
                            );
                        }

                        return item.href ? (
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
                        );
                    })}
                </nav>

                <div className="sidebar-footnote-block">
                    {isAuthenticated ? (
                        <p className="sidebar-session-badge">
                            Signed in as {currentUserDisplayName}
                        </p>
                    ) : null}
                    <p className="sidebar-footnote">
                        Resume will eventually include the Work History subsection.
                    </p>
                </div>
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
