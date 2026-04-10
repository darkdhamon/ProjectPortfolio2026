import { formatProjectDates } from '../../appSupport';
import { usePortfolioProfile } from '../../hooks/usePortfolioProfile';
import { useWorkHistory } from '../../hooks/useWorkHistory';

function getRoleCount(employerCount: number, roleCount: number) {
    return `${roleCount} role${roleCount === 1 ? '' : 's'} across ${employerCount} employer${employerCount === 1 ? '' : 's'}`;
}

function getResumeContactHref(value: string) {
    if (value.startsWith('http://') || value.startsWith('https://')) {
        return value;
    }

    if (value.includes('@')) {
        return `mailto:${value}`;
    }

    return '';
}

export function ResumePage() {
    const {
        profile,
        isLoading: isProfileLoading,
        error: profileError,
        isMissing: isProfileMissing
    } = usePortfolioProfile();
    const {
        employers,
        isLoading: isWorkHistoryLoading,
        error: workHistoryError
    } = useWorkHistory();

    const roleCount = employers.reduce((total, employer) => total + employer.jobRoles.length, 0);
    const location = employers
        .map(employer => [employer.city, employer.region].filter(Boolean).join(', '))
        .find(value => value.length > 0);
    const primaryRole = employers.flatMap(employer => employer.jobRoles).find(role => role.role.trim().length > 0);
    const contactMethods = (profile?.contactMethods ?? []).slice(0, 3);
    const socialLinks = (profile?.socialLinks ?? []).slice(0, 2);
    const sampleEmployers = employers.slice(0, 3);
    const isLoading = isProfileLoading || isWorkHistoryLoading;
    const errors = [profileError, workHistoryError].filter(Boolean);

    return (
        <main className="resume-page">
            {errors.map(error => (
                <p key={error} className="status-banner error">{error}</p>
            ))}
            {!errors.length && isLoading ? (
                <p className="status-banner">Loading resume shell...</p>
            ) : null}

            <section className="hero-panel resume-hero">
                <div className="resume-hero-copy">
                    <p className="eyebrow">Public Resume</p>
                    <h1>{profile?.displayName ?? 'Resume shell ready for public portfolio data.'}</h1>
                    <p className="hero-description">
                        {primaryRole?.role
                            ? `${primaryRole.role}${location ? ` based in ${location}` : ''}. This recruiter-focused view is built to stay concise, skimmable, and ready for richer structured sections.`
                            : 'This page establishes the recruiter-focused resume shell so structured sections, filtering, and export actions can plug in without reworking the layout.'}
                    </p>
                </div>

                <div className="resume-hero-meta">
                    <section className="resume-callout-card" aria-label="Resume summary">
                        <span className="stat-label">Resume Snapshot</span>
                        <strong>{roleCount > 0 ? getRoleCount(employers.length, roleCount) : 'Waiting for published resume data'}</strong>
                        <p>
                            {profile?.availabilityHeadline
                                ? profile.availabilityHeadline
                                : 'Publish public profile and work history records to turn this shell into a complete recruiter-facing resume.'}
                        </p>
                    </section>

                    <div className="hero-stats" aria-label="Resume summary stats">
                        <div className="stat-card">
                            <span className="stat-label">Employers</span>
                            <strong>{employers.length}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Roles</span>
                            <strong>{roleCount}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Contact Channels</span>
                            <strong>{contactMethods.length + socialLinks.length}</strong>
                        </div>
                    </div>
                </div>
            </section>

            <section className="resume-grid">
                <article className="resume-panel resume-panel-emphasis">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Header</p>
                        <h2>Recruiter essentials up front.</h2>
                    </div>

                    {contactMethods.length > 0 || socialLinks.length > 0 ? (
                        <div className="resume-link-list">
                            {contactMethods.map(method => {
                                const href = method.href ?? getResumeContactHref(method.value);
                                return href ? (
                                    <a
                                        key={`${method.type}-${method.label}-${method.sortOrder}`}
                                        className="resume-link-card"
                                        href={href}
                                        target="_blank"
                                        rel="noreferrer">
                                        <span className="meta-label">{method.label}</span>
                                        <strong>{method.value}</strong>
                                        {method.note ? <p>{method.note}</p> : null}
                                    </a>
                                ) : (
                                    <div key={`${method.type}-${method.label}-${method.sortOrder}`} className="resume-link-card">
                                        <span className="meta-label">{method.label}</span>
                                        <strong>{method.value}</strong>
                                        {method.note ? <p>{method.note}</p> : null}
                                    </div>
                                );
                            })}

                            {socialLinks.map(link => (
                                <a
                                    key={`${link.platform}-${link.label}-${link.sortOrder}`}
                                    className="resume-link-card"
                                    href={link.url}
                                    target="_blank"
                                    rel="noreferrer">
                                    <span className="meta-label">{link.label}</span>
                                    <strong>{link.handle ?? link.label}</strong>
                                    {link.summary ? <p>{link.summary}</p> : null}
                                </a>
                            ))}
                        </div>
                    ) : (
                        <p className="secondary-copy">
                            Public contact and social links will appear here once the portfolio profile is configured.
                        </p>
                    )}
                </article>

                <article className="resume-panel">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Profile Section</p>
                        <h2>Summary area reserved for concise positioning.</h2>
                    </div>

                    <div className="resume-shell-note">
                        <p>
                            {profile?.contactIntro
                                ? profile.contactIntro
                                : 'A short recruiter-facing profile summary will land here once the public portfolio profile is ready for resume presentation.'}
                        </p>
                    </div>
                </article>
            </section>

            <section className="resume-grid resume-grid-secondary">
                <article className="resume-panel">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Experience Shell</p>
                        <h2>Space reserved for condensed role history.</h2>
                    </div>

                    {sampleEmployers.length > 0 ? (
                        <div className="resume-timeline">
                            {sampleEmployers.map(employer => {
                                const latestRole = employer.jobRoles[0];
                                return (
                                    <section key={employer.id} className="resume-timeline-item">
                                        <div className="resume-timeline-heading">
                                            <div>
                                                <h3>{employer.name}</h3>
                                                <p>{latestRole?.role ?? 'Published role details will appear here.'}</p>
                                            </div>
                                            {latestRole ? (
                                                <span className="resume-date-range">
                                                    {formatProjectDates(latestRole.startDate, latestRole.endDate)}
                                                </span>
                                            ) : null}
                                        </div>
                                        <p className="helper-copy">
                                            Full recruiter-focused role bullets, skill highlights, and optional sections are intentionally deferred to the next resume issue.
                                        </p>
                                    </section>
                                );
                            })}
                        </div>
                    ) : (
                        <p className="secondary-copy">
                            Published work history will appear here once employer and job-role records are available.
                        </p>
                    )}
                </article>

                <article className="resume-panel">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Skills Shell</p>
                        <h2>Reserved for scan-friendly capability highlights.</h2>
                    </div>

                    <div className="resume-shell-note">
                        <p>
                            Skill and technology emphasis will plug into this panel after the richer structured resume rendering work is complete.
                        </p>
                    </div>
                </article>
            </section>

            <section className="resume-action-area">
                <article className="resume-panel resume-action-panel">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Action Area</p>
                        <h2>Future actions already have a home.</h2>
                    </div>

                    <div className="resume-action-grid">
                        <div className="resume-action-card">
                            <span className="meta-label">PDF Export</span>
                            <strong>Planned under issue #88</strong>
                            <p>An ATS-friendly export action will attach to this shell without changing the page structure.</p>
                        </div>
                        <div className="resume-action-card">
                            <span className="meta-label">Static Resume Link</span>
                            <strong>{isProfileMissing ? 'Not configured' : 'Awaiting configuration support'}</strong>
                            <p>Optional external resume delivery will surface here once the admin configuration work is available.</p>
                        </div>
                    </div>
                </article>
            </section>
        </main>
    );
}
