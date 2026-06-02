import type { ReactNode } from 'react';
import type { NavigateFn } from '../../app/navigation';
import { InternalLink } from '../common/InternalLink';
import { adminSections, getAdminSection, type AdminSectionId } from './adminSections';

const sectionDetails: Record<Exclude<AdminSectionId, 'dashboard'>, { title: string; body: string; bullets: string[]; primaryActionLabel: string; }> = {
    projects: {
        title: 'Project authoring routes are now reserved.',
        body: 'The projects area exists now as a stable destination for the create, edit, archive, and featured-project workflows that land in later issues.',
        bullets: [
            'Issue #65 can attach project creation and editing forms without replacing the shell.',
            'Issue #23 can add archive or delete controls into a dedicated project management context.',
            'Issue #103 can extend the same section with featured ordering controls.'
        ],
        primaryActionLabel: 'Return to overview'
    },
    'social-links': {
        title: 'Social link management has a permanent home.',
        body: 'The social links route keeps public profile destinations separated from account settings so future admin editing stays focused on portfolio content.',
        bullets: [
            'Issue #66 can add CRUD controls for social links in this section.',
            'The public contact page can keep consuming the same structured data source.',
            'Navigation and route handling are already wired for future form state.'
        ],
        primaryActionLabel: 'Return to overview'
    },
    resume: {
        title: 'Resume configuration can build on a stable shell.',
        body: 'This section is reserved for the admin controls that shape the structured resume experience, public resume actions, and later import source settings.',
        bullets: [
            'Issue #67 can add resume configuration UI without changing the surrounding navigation.',
            'The route aligns with the public resume work tracked under issue #61.',
            'Later resume import tasks can link back here as configuration grows.'
        ],
        primaryActionLabel: 'Return to overview'
    },
    publishing: {
        title: 'Draft and preview workflows have an explicit route.',
        body: 'Publishing concerns now have a dedicated section so preview and publish behavior can expand without being bolted onto unrelated admin pages.',
        bullets: [
            'Issue #68 can add draft state controls and preview actions in this workspace.',
            'Later content modules can point to one shared publishing destination.',
            'The shell keeps the route discoverable even before the live workflow is implemented.'
        ],
        primaryActionLabel: 'Return to overview'
    }
};

export function AdminWorkspacePage({
    activeSection,
    currentUserDisplayName,
    onNavigate,
    sectionContent
}: {
    activeSection: AdminSectionId;
    currentUserDisplayName: string;
    onNavigate: NavigateFn;
    sectionContent?: ReactNode;
}) {
    const currentSection = getAdminSection(activeSection);

    return (
        <main className="admin-page">
            <section className="admin-hero admin-workspace-hero">
                <div>
                    <p className="eyebrow">{currentSection.kicker}</p>
                    <h1>{currentSection.title}</h1>
                    <p className="hero-description">
                        {currentSection.summary}
                    </p>
                </div>

                <div className="admin-callout">
                    <span className="stat-label">Current admin</span>
                    <strong>{currentUserDisplayName}</strong>
                    <p className="secondary-copy">Management areas wired in this shell: {adminSections.length}</p>
                </div>
            </section>

            <section className="admin-workspace-shell">
                <aside className="admin-card admin-workspace-nav">
                    <div className="admin-workspace-nav-copy">
                        <p className="eyebrow">Navigation</p>
                        <h2>Planned content areas</h2>
                        <p>These routes are now stable, so follow-on issues can plug live modules into the shell instead of redefining admin navigation.</p>
                    </div>

                    <nav className="admin-section-nav" aria-label="Admin workspace sections">
                        {adminSections.map(section => (
                            <InternalLink
                                key={section.id}
                                className={`admin-section-link${section.id === activeSection ? ' active' : ''}`}
                                href={section.href}
                                ariaCurrent={section.id === activeSection ? 'page' : undefined}
                                onNavigate={onNavigate}
                                preserveScroll={true}>
                                <span>{section.title}</span>
                                <span className="admin-route-pill">{section.issueReferences}</span>
                            </InternalLink>
                        ))}
                    </nav>

                    <div className="admin-workspace-utility">
                        <p className="eyebrow">Session</p>
                        <p>Account maintenance stays available from the same admin surface while the content tools grow.</p>
                        <InternalLink
                            className="admin-action-link"
                            href="/admin/account"
                            onNavigate={onNavigate}
                            preserveScroll={true}>
                            Open Account Settings
                        </InternalLink>
                    </div>
                </aside>

                <div className="admin-workspace-content">
                    {activeSection === 'dashboard' ? (
                        <section className="admin-dashboard-grid">
                            {adminSections.filter(section => section.id !== 'dashboard').map(section => (
                                <article key={section.id} className="admin-card admin-section-panel">
                                    <p className="eyebrow">{section.kicker}</p>
                                    <h2>{section.title}</h2>
                                    <p>{section.summary}</p>
                                    <p className="admin-section-meta">Next tracked work: {section.issueReferences}</p>
                                    <InternalLink
                                        className="admin-action-link"
                                        href={section.href}
                                        onNavigate={onNavigate}
                                        preserveScroll={true}>
                                        Open {section.title}
                                    </InternalLink>
                                </article>
                            ))}
                        </section>
                    ) : sectionContent ? (
                        <>
                            {activeSection === 'resume' ? (
                                <div className="admin-inline-actions">
                                    <InternalLink
                                        className="admin-action-link"
                                        href="/admin/resume/import"
                                        onNavigate={onNavigate}
                                        preserveScroll={true}>
                                        Start Resume Import
                                    </InternalLink>
                                </div>
                            ) : null}
                            {sectionContent}
                        </>
                    ) : (
                        <article className="admin-card admin-section-panel">
                            <p className="eyebrow">{currentSection.kicker}</p>
                            <h2>{sectionDetails[activeSection].title}</h2>
                            <p>{sectionDetails[activeSection].body}</p>
                            <ul className="admin-section-list">
                                {sectionDetails[activeSection].bullets.map(item => (
                                    <li key={item}>{item}</li>
                                ))}
                            </ul>
                            <p className="admin-section-meta">Tracked issues: {currentSection.issueReferences}</p>
                            {activeSection === 'resume' ? (
                                <div className="admin-inline-actions">
                                    <InternalLink
                                        className="admin-action-link"
                                        href="/admin/resume/import"
                                        onNavigate={onNavigate}
                                        preserveScroll={true}>
                                        Start Resume Import
                                    </InternalLink>
                                </div>
                            ) : null}
                            <InternalLink
                                className="admin-action-link"
                                href="/admin"
                                onNavigate={onNavigate}
                                preserveScroll={true}>
                                {sectionDetails[activeSection].primaryActionLabel}
                            </InternalLink>
                        </article>
                    )}
                </div>
            </section>
        </main>
    );
}
