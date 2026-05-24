import type { JobRole } from '../../app/types';
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

function getOrderedUniqueValues(values: string[]) {
    const seen = new Set<string>();

    return values.filter(value => {
        const trimmedValue = value.trim();
        if (!trimmedValue || seen.has(trimmedValue)) {
            return false;
        }

        seen.add(trimmedValue);
        return true;
    });
}

function getLocationLabel(city?: string | null, region?: string | null) {
    const parts = [city?.trim(), region?.trim()].filter(part => part && part.length > 0);
    return parts.length > 0 ? parts.join(', ') : null;
}

function getEmployerDateRange(jobRoles: JobRole[]) {
    if (jobRoles.length === 0) {
        return null;
    }

    const newestRole = jobRoles[0];
    const oldestRole = jobRoles[jobRoles.length - 1];
    return formatProjectDates(oldestRole.startDate, newestRole.endDate);
}

function getDescriptionSummary(markdown: string) {
    return markdown
        .split(/\r?\n\r?\n/)
        .map(paragraph => paragraph.trim().replace(/^#+\s*/, ''))
        .find(paragraph => paragraph.length > 0) ?? '';
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

    const roleRecords = employers.flatMap(employer => employer.jobRoles.map(jobRole => ({
        employer,
        jobRole
    })));
    const roleCount = roleRecords.length;
    const primaryRole = roleRecords.find(record => record.jobRole.role.trim().length > 0)?.jobRole;
    const location = employers
        .map(employer => getLocationLabel(employer.city, employer.region))
        .find(Boolean);
    const contactMethods = (profile?.contactMethods ?? []).slice(0, 3);
    const socialLinks = (profile?.socialLinks ?? []).slice(0, 2);
    const contactChannelCount = contactMethods.length + socialLinks.length;
    const summaryParagraphs = [profile?.contactIntro?.trim(), profile?.availabilitySummary?.trim()].filter(
        (value): value is string => Boolean(value && value.length > 0)
    );
    const skillHighlights = getOrderedUniqueValues(roleRecords.flatMap(record => record.jobRole.skills)).slice(0, 8);
    const technologyHighlights = getOrderedUniqueValues(roleRecords.flatMap(record => record.jobRole.technologies)).slice(0, 8);
    const hasSummarySection = Boolean(profile?.contactHeadline?.trim() || profile?.availabilityHeadline?.trim() || summaryParagraphs.length > 0);
    const hasSkillsSection = skillHighlights.length > 0 || technologyHighlights.length > 0;
    const isLoading = isProfileLoading || isWorkHistoryLoading;
    const errors = [profileError, workHistoryError].filter(Boolean);

    return (
        <main className="resume-page">
            {errors.map(error => (
                <p key={error} className="status-banner error">{error}</p>
            ))}
            {!errors.length && isLoading ? (
                <p className="status-banner">Loading resume...</p>
            ) : null}

            <section className="hero-panel resume-hero">
                <div className="resume-hero-copy">
                    <p className="eyebrow">Public Resume</p>
                    <h1>{profile?.displayName ?? 'Resume shell ready for public portfolio data.'}</h1>
                    <p className="hero-description">
                        {profile?.contactHeadline?.trim()
                            ? profile.contactHeadline
                            : primaryRole?.role
                                ? `${primaryRole.role}${location ? ` based in ${location}` : ''}. This recruiter-focused view stays concise while turning published portfolio data into a skimmable resume.`
                                : 'This page turns published profile and work-history data into a recruiter-facing resume layout once those records are available.'}
                    </p>
                </div>

                <div className="resume-hero-meta">
                    <section className="resume-callout-card" aria-label="Resume summary">
                        <span className="stat-label">Resume Snapshot</span>
                        <strong>{roleCount > 0 ? getRoleCount(employers.length, roleCount) : 'Waiting for published resume data'}</strong>
                        <p>
                            {profile?.availabilityHeadline?.trim()
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
                            <strong>{contactChannelCount}</strong>
                        </div>
                    </div>
                </div>
            </section>

            <section className="resume-action-area">
                <article className="resume-panel resume-action-panel">
                    <p className="eyebrow">Resume Actions</p>
                    <div className="resume-action-grid">
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Resume Filters</span>
                                <span className="resume-action-note">Issue #87</span>
                            </div>
                            <button
                                className="resume-action-button"
                                type="button"
                                disabled
                                aria-disabled="true"
                                aria-label="Filter Resume">
                                <span>Filter Resume</span>
                                <span className="coming-soon-pill">Coming Soon</span>
                            </button>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Skill Highlights</span>
                                <span className="resume-action-note">Issue #87</span>
                            </div>
                            <button
                                className="resume-action-button"
                                type="button"
                                disabled
                                aria-disabled="true"
                                aria-label="Highlight Skills">
                                <span>Highlight Skills</span>
                                <span className="coming-soon-pill">Coming Soon</span>
                            </button>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">PDF Export</span>
                                <span className="resume-action-note">Issue #88</span>
                            </div>
                            <button
                                className="resume-action-button"
                                type="button"
                                disabled
                                aria-disabled="true"
                                aria-label="Download PDF">
                                <span>Download PDF</span>
                                <span className="coming-soon-pill">Coming Soon</span>
                            </button>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Static Resume Link</span>
                                <span className="resume-action-note">{isProfileMissing ? 'Needs config' : 'Planned'}</span>
                            </div>
                            <button
                                className="resume-action-button"
                                type="button"
                                disabled
                                aria-disabled="true"
                                aria-label="Open Static Resume">
                                <span>Open Static Resume</span>
                                <span className="coming-soon-pill">Coming Soon</span>
                            </button>
                        </div>
                    </div>
                </article>
            </section>

            <section className="resume-grid">
                <article className="resume-panel resume-panel-emphasis">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Header</p>
                        <h2>Contact and portfolio links.</h2>
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

                {hasSummarySection ? (
                    <article className="resume-panel">
                        <div className="resume-panel-heading">
                            <p className="eyebrow">Career Summary</p>
                            <h2>Positioning for recruiters and hiring teams.</h2>
                        </div>

                        <div className="resume-summary-stack">
                            {profile?.contactHeadline?.trim() ? (
                                <div className="resume-summary-card">
                                    <span className="meta-label">Headline</span>
                                    <strong>{profile.contactHeadline}</strong>
                                </div>
                            ) : null}
                            {profile?.availabilityHeadline?.trim() ? (
                                <div className="resume-summary-card">
                                    <span className="meta-label">Availability</span>
                                    <strong>{profile.availabilityHeadline}</strong>
                                </div>
                            ) : null}
                            {summaryParagraphs.map(paragraph => (
                                <p key={paragraph} className="secondary-copy">{paragraph}</p>
                            ))}
                        </div>
                    </article>
                ) : null}
            </section>

            <section className="resume-grid resume-grid-secondary">
                <article className="resume-panel">
                    <div className="resume-panel-heading">
                        <p className="eyebrow">Experience</p>
                        <h2>Condensed work history.</h2>
                    </div>

                    {employers.length > 0 ? (
                        <div className="resume-experience-list">
                            {employers.map(employer => {
                                const employerLocation = getLocationLabel(employer.city, employer.region);
                                const employerRange = getEmployerDateRange(employer.jobRoles);

                                return (
                                    <section key={employer.id} className="resume-experience-group">
                                        <div className="resume-experience-header">
                                            <div>
                                                <h3>{employer.name}</h3>
                                                {employerLocation ? <p>{employerLocation}</p> : null}
                                            </div>
                                            {employerRange ? (
                                                <span className="resume-date-range">{employerRange}</span>
                                            ) : null}
                                        </div>

                                        <div className="resume-experience-role-list">
                                            {employer.jobRoles.map(jobRole => {
                                                const summary = getDescriptionSummary(jobRole.descriptionMarkdown);

                                                return (
                                                    <article
                                                        key={`${employer.id}-${jobRole.role}-${jobRole.startDate}`}
                                                        className="resume-experience-role">
                                                        <div className="resume-role-meta">
                                                            <div>
                                                                <strong>{jobRole.role}</strong>
                                                                <p>{formatProjectDates(jobRole.startDate, jobRole.endDate)}</p>
                                                            </div>
                                                        </div>

                                                        {summary ? (
                                                            <p className="resume-role-copy">{summary}</p>
                                                        ) : null}

                                                        {jobRole.skills.length > 0 ? (
                                                            <div className="tag-group" aria-label={`${jobRole.role} skills`}>
                                                                {jobRole.skills.map(skill => (
                                                                    <span key={skill} className="tag skill">{skill}</span>
                                                                ))}
                                                            </div>
                                                        ) : null}

                                                        {jobRole.technologies.length > 0 ? (
                                                            <div className="tag-group secondary" aria-label={`${jobRole.role} technologies`}>
                                                                {jobRole.technologies.map(technology => (
                                                                    <span key={technology} className="tag technology">{technology}</span>
                                                                ))}
                                                            </div>
                                                        ) : null}
                                                    </article>
                                                );
                                            })}
                                        </div>
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

                {hasSkillsSection ? (
                    <article className="resume-panel">
                        <div className="resume-panel-heading">
                            <p className="eyebrow">Skills</p>
                            <h2>Core skills and technologies.</h2>
                        </div>

                        <div className="resume-highlight-list">
                            {skillHighlights.length > 0 ? (
                                <section className="resume-highlight-group">
                                    <span className="meta-label">Skills</span>
                                    <div className="tag-group" aria-label="Resume skills">
                                        {skillHighlights.map(skill => (
                                            <span key={skill} className="tag skill">{skill}</span>
                                        ))}
                                    </div>
                                </section>
                            ) : null}

                            {technologyHighlights.length > 0 ? (
                                <section className="resume-highlight-group">
                                    <span className="meta-label">Technologies</span>
                                    <div className="tag-group secondary" aria-label="Resume technologies">
                                        {technologyHighlights.map(technology => (
                                            <span key={technology} className="tag technology">{technology}</span>
                                        ))}
                                    </div>
                                </section>
                            ) : null}
                        </div>
                    </article>
                ) : null}
            </section>
        </main>
    );
}
