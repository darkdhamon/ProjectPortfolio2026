import { useState } from 'react';
import type { JobRole } from '../../app/types';
import { formatProjectDates } from '../../appSupport';
import { usePortfolioProfile } from '../../hooks/usePortfolioProfile';
import { useResumeConfiguration } from '../../hooks/useResumeConfiguration';
import { useWorkHistory } from '../../hooks/useWorkHistory';
import { downloadResumePdf } from './resumePdf';

interface ResumeDateValue {
    label: string;
    title: string;
}

interface ResumeRoleEntry {
    jobRole: JobRole;
    key: string;
    dateValue: ResumeDateValue;
}

interface ResumeHighlight {
    count: number;
    label: string;
}

type ResumeTimeFilterKey = 'all' | 'last10Years' | 'last5Years';
type ResumeEmployerLimitKey = 'all' | '3' | '5';

const resumeTimeFilters: ReadonlyArray<{
    description: string;
    key: ResumeTimeFilterKey;
    label: string;
    years: number | null;
}> = [
    {
        key: 'all',
        label: 'Full history',
        years: null,
        description: 'Every published role and employer.'
    },
    {
        key: 'last10Years',
        label: 'Last 10 years',
        years: 10,
        description: 'Roles active within the last decade.'
    },
    {
        key: 'last5Years',
        label: 'Last 5 years',
        years: 5,
        description: 'Recent experience for fast recruiter scans.'
    }
];

const resumeEmployerLimits: ReadonlyArray<{
    description: string;
    key: ResumeEmployerLimitKey;
    label: string;
    limit: number | null;
}> = [
    {
        key: 'all',
        label: 'All employers',
        limit: null,
        description: 'Show every employer in the active time window.'
    },
    {
        key: '3',
        label: 'Top 3',
        limit: 3,
        description: 'Focus on the three most recent employers.'
    },
    {
        key: '5',
        label: 'Top 5',
        limit: 5,
        description: 'Keep the resume compact without hiding too much depth.'
    }
];

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

function getLocationLabel(city?: string | null, region?: string | null) {
    const parts = [city?.trim(), region?.trim()].filter(part => part && part.length > 0);
    return parts.length > 0 ? parts.join(', ') : null;
}

function getDateMonthValue(value: string) {
    const [year, month] = value.split('-').map(Number);
    return {
        year,
        month
    };
}

function getMonthDistance(startDate: string, endDate?: string | null) {
    const start = getDateMonthValue(startDate);
    const end = endDate ? getDateMonthValue(endDate) : {
        year: new Date().getFullYear(),
        month: new Date().getMonth() + 1
    };

    return Math.max(0, ((end.year - start.year) * 12) + (end.month - start.month) + 1);
}

function getMonthIndex(value: string) {
    const { year, month } = getDateMonthValue(value);
    return (year * 12) + month;
}

function formatDateKey(date: Date) {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function formatDurationLabel(totalMonths: number) {
    const years = Math.floor(totalMonths / 12);
    const months = totalMonths % 12;
    const parts: string[] = [];

    if (years > 0) {
        parts.push(`${years} year${years === 1 ? '' : 's'}`);
    }

    if (months > 0 || parts.length === 0) {
        parts.push(`${months} month${months === 1 ? '' : 's'}`);
    }

    return parts.join(', ');
}

function createDateValue(startDate: string, endDate: string | null | undefined, titlePrefix: string): ResumeDateValue {
    return {
        label: formatProjectDates(startDate, endDate),
        title: `${titlePrefix}: ${formatDurationLabel(getMonthDistance(startDate, endDate))}`
    };
}

function getEmployerDateValue(employerName: string, jobRoles: JobRole[]) {
    if (jobRoles.length === 0) {
        return null;
    }

    const earliestRole = jobRoles.reduce((currentEarliest, role) => role.startDate < currentEarliest.startDate ? role : currentEarliest);
    const activeRole = jobRoles.find(role => !role.endDate);
    const latestCompletedRole = jobRoles.reduce((currentLatest, role) => {
        if (!role.endDate) {
            return currentLatest;
        }

        if (!currentLatest || role.endDate > (currentLatest.endDate ?? '')) {
            return role;
        }

        return currentLatest;
    }, null as JobRole | null);
    const latestEndDate = activeRole ? null : latestCompletedRole?.endDate;

    return createDateValue(earliestRole.startDate, latestEndDate, `Time at ${employerName}`);
}

function getDescriptionSummary(markdown: string) {
    return markdown
        .split(/\r?\n\r?\n/)
        .map(paragraph => paragraph.trim().replace(/^#+\s*/, ''))
        .find(paragraph => paragraph.length > 0) ?? '';
}

function getJobRoleKeySignature(jobRole: JobRole) {
    return [
        jobRole.role,
        jobRole.startDate,
        jobRole.endDate ?? 'present',
        jobRole.supervisorName ?? '',
        jobRole.descriptionMarkdown,
        jobRole.skills.join(','),
        jobRole.technologies.join(',')
    ].join('|');
}

function buildResumeRoleEntries(jobRoles: JobRole[]) {
    const seenSignatures = new Map<string, number>();

    return jobRoles.map(jobRole => {
        const signature = getJobRoleKeySignature(jobRole);
        const occurrence = (seenSignatures.get(signature) ?? 0) + 1;
        seenSignatures.set(signature, occurrence);

        return {
            jobRole,
            key: `${signature}|${occurrence}`,
            dateValue: createDateValue(jobRole.startDate, jobRole.endDate, 'Time in role')
        };
    });
}

function roleMatchesTimeFilter(jobRole: JobRole, cutoffDateKey: string | null, currentDateKey: string) {
    if (!cutoffDateKey) {
        return true;
    }

    const roleEndDateKey = jobRole.endDate ?? currentDateKey;
    return roleEndDateKey >= cutoffDateKey;
}

function getEmployerRecencyScore(jobRoles: JobRole[], currentMonthIndex: number) {
    return jobRoles.reduce((highestScore, jobRole) => {
        const roleScore = jobRole.endDate ? getMonthIndex(jobRole.endDate) : currentMonthIndex;
        return Math.max(highestScore, roleScore);
    }, 0);
}

function buildResumeHighlights(
    roleEntries: Array<{ jobRole: JobRole }>,
    getValues: (jobRole: JobRole) => string[],
    limit: number
) {
    const counts = new Map<string, { count: number; firstSeenAt: number }>();

    roleEntries.forEach((entry, entryIndex) => {
        getValues(entry.jobRole).forEach(value => {
            const trimmedValue = value.trim();
            if (!trimmedValue) {
                return;
            }

            const currentValue = counts.get(trimmedValue);
            if (currentValue) {
                currentValue.count += 1;
                return;
            }

            counts.set(trimmedValue, {
                count: 1,
                firstSeenAt: entryIndex
            });
        });
    });

    return [...counts.entries()]
        .sort((left, right) => {
            if (right[1].count !== left[1].count) {
                return right[1].count - left[1].count;
            }

            if (left[1].firstSeenAt !== right[1].firstSeenAt) {
                return left[1].firstSeenAt - right[1].firstSeenAt;
            }

            return left[0].localeCompare(right[0]);
        })
        .slice(0, limit)
        .map(([label, metadata]) => ({
            label,
            count: metadata.count
        } satisfies ResumeHighlight));
}

function getResumeViewSummary(
    selectedTimeFilter: { description: string; key: ResumeTimeFilterKey; label: string },
    selectedEmployerLimit: { description: string; key: ResumeEmployerLimitKey; label: string; limit: number | null },
    employerCount: number,
    roleCount: number,
    totalRoleCount: number
) {
    if (totalRoleCount === 0) {
        return 'Publish public profile and work history records to turn this shell into a complete recruiter-facing resume.';
    }

    if (roleCount === 0) {
        return 'No published roles match the active resume filters yet. Expand the time window or increase the employer count to bring older experience back into view.';
    }

    const timeSummary = selectedTimeFilter.key === 'all'
        ? 'Showing full published history.'
        : `Showing ${selectedTimeFilter.label.toLowerCase()}.`;
    const employerSummary = selectedEmployerLimit.limit === null
        ? 'All matching employers remain visible.'
        : `Limited to the ${employerCount} most recent matching employer${employerCount === 1 ? '' : 's'}.`;

    return `${timeSummary} ${employerSummary} ${getRoleCount(employerCount, roleCount)} remain in view.`;
}

export function ResumePage() {
    const [selectedTimeFilterKey, setSelectedTimeFilterKey] = useState<ResumeTimeFilterKey>('all');
    const [selectedEmployerLimitKey, setSelectedEmployerLimitKey] = useState<ResumeEmployerLimitKey>('all');
    const [pdfExportFeedback, setPdfExportFeedback] = useState<string | null>(null);
    const [pdfExportError, setPdfExportError] = useState<string | null>(null);
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
    const {
        configuration: resumeConfiguration,
        isLoading: isResumeConfigurationLoading,
        error: resumeConfigurationError,
        isMissing: isResumeConfigurationMissing
    } = useResumeConfiguration();

    const currentDate = new Date();
    const currentMonthIndex = (currentDate.getFullYear() * 12) + currentDate.getMonth() + 1;
    const selectedTimeFilter = resumeTimeFilters.find(filter => filter.key === selectedTimeFilterKey) ?? resumeTimeFilters[0];
    const selectedEmployerLimit = resumeEmployerLimits.find(limit => limit.key === selectedEmployerLimitKey) ?? resumeEmployerLimits[0];
    const cutoffDate = selectedTimeFilter.years === null
        ? null
        : new Date(currentDate.getFullYear() - selectedTimeFilter.years, currentDate.getMonth(), currentDate.getDate());
    const cutoffDateKey = cutoffDate ? formatDateKey(cutoffDate) : null;
    const currentDateKey = formatDateKey(currentDate);
    const employersInTimeView = employers
        .map(employer => ({
            ...employer,
            jobRoles: employer.jobRoles.filter(jobRole => roleMatchesTimeFilter(jobRole, cutoffDateKey, currentDateKey))
        }))
        .filter(employer => employer.jobRoles.length > 0)
        .sort((left, right) => getEmployerRecencyScore(right.jobRoles, currentMonthIndex) - getEmployerRecencyScore(left.jobRoles, currentMonthIndex));
    const filteredEmployers = selectedEmployerLimit.limit === null
        ? employersInTimeView
        : employersInTimeView.slice(0, selectedEmployerLimit.limit);
    const roleRecords = employers.flatMap(employer => employer.jobRoles.map(jobRole => ({
        employer,
        jobRole
    })));
    const filteredRoleRecords = filteredEmployers.flatMap(employer => employer.jobRoles.map(jobRole => ({
        employer,
        jobRole
    })));
    const totalEmployerCount = employers.length;
    const totalRoleCount = roleRecords.length;
    const filteredRoleCount = filteredRoleRecords.length;
    const primaryRole = filteredRoleRecords.find(record => record.jobRole.role.trim().length > 0)?.jobRole
        ?? roleRecords.find(record => record.jobRole.role.trim().length > 0)?.jobRole;
    const location = filteredEmployers
        .map(employer => getLocationLabel(employer.city, employer.region))
        .find(Boolean)
        ?? employers
            .map(employer => getLocationLabel(employer.city, employer.region))
            .find(Boolean);
    const contactMethods = (profile?.contactMethods ?? []).slice(0, 3);
    const socialLinks = (profile?.socialLinks ?? []).slice(0, 2);
    const contactChannelCount = contactMethods.length + socialLinks.length;
    const summaryParagraphs = [profile?.contactIntro?.trim(), profile?.availabilitySummary?.trim()].filter(
        (value): value is string => Boolean(value && value.length > 0)
    );
    const skillHighlights = buildResumeHighlights(filteredRoleRecords, jobRole => jobRole.skills, 6);
    const technologyHighlights = buildResumeHighlights(filteredRoleRecords, jobRole => jobRole.technologies, 6);
    const highlightedSkillSet = new Set(skillHighlights.map(highlight => highlight.label));
    const highlightedTechnologySet = new Set(technologyHighlights.map(highlight => highlight.label));
    const hasSummarySection = Boolean(profile?.contactHeadline?.trim() || profile?.availabilityHeadline?.trim() || summaryParagraphs.length > 0);
    const hasSkillsSection = skillHighlights.length > 0 || technologyHighlights.length > 0;
    const isLoading = isProfileLoading || isWorkHistoryLoading || isResumeConfigurationLoading;
    const errors = [profileError, workHistoryError, resumeConfigurationError].filter(Boolean);
    const activeFilterCount = (selectedTimeFilter.key === 'all' ? 0 : 1) + (selectedEmployerLimit.key === 'all' ? 0 : 1);
    const resumeViewSummary = getResumeViewSummary(
        selectedTimeFilter,
        selectedEmployerLimit,
        filteredEmployers.length,
        filteredRoleCount,
        totalRoleCount
    );
    const resumeSourceTypeLabel = resumeConfiguration?.sourceType === 'embed' ? 'Embed source' : 'Hosted file';
    const canExportPdf = !isLoading && errors.length === 0;

    function handleDownloadPdf() {
        try {
            downloadResumePdf({
                profile,
                employers: filteredEmployers,
                activeTimeWindowLabel: selectedTimeFilter.label,
                activeEmployerLimitLabel: selectedEmployerLimit.label,
                viewSummary: resumeViewSummary,
                generatedAt: new Date(),
                skillHighlights: skillHighlights.map(highlight => highlight.label),
                technologyHighlights: technologyHighlights.map(highlight => highlight.label)
            });
            setPdfExportFeedback('PDF download started.');
            setPdfExportError(null);
        } catch {
            setPdfExportFeedback(null);
            setPdfExportError('Unable to export the resume PDF right now.');
        }
    }

    return (
        <main className="resume-page">
            {errors.map(error => (
                <p key={error} className="status-banner error">{error}</p>
            ))}
            {pdfExportError ? (
                <p className="status-banner error">{pdfExportError}</p>
            ) : null}
            {pdfExportFeedback ? (
                <p className="status-banner">{pdfExportFeedback}</p>
            ) : null}
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
                        <strong>{filteredRoleCount > 0 ? getRoleCount(filteredEmployers.length, filteredRoleCount) : totalRoleCount > 0 ? 'No roles match the current resume filters' : 'Waiting for published resume data'}</strong>
                        <p>
                            {profile?.availabilityHeadline?.trim()
                                ? profile.availabilityHeadline
                                : resumeViewSummary}
                        </p>
                    </section>

                    <div className="hero-stats" aria-label="Resume summary stats">
                        <div className="stat-card">
                            <span className="stat-label">{activeFilterCount > 0 ? 'Visible Employers' : 'Employers'}</span>
                            <strong>{filteredEmployers.length}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">{activeFilterCount > 0 ? 'Visible Roles' : 'Roles'}</span>
                            <strong>{filteredRoleCount}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">{activeFilterCount > 0 ? 'Active Filters' : 'Contact Channels'}</span>
                            <strong>{activeFilterCount > 0 ? activeFilterCount : contactChannelCount}</strong>
                        </div>
                    </div>
                </div>
            </section>

            <section className="resume-action-area">
                <article className="resume-panel resume-action-panel">
                    <p className="eyebrow">Resume Controls</p>
                    <div className="resume-action-grid">
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Time Window</span>
                                <span className="resume-action-note">{selectedTimeFilter.description}</span>
                            </div>
                            <div className="resume-filter-chip-list" role="group" aria-label="Resume time filters">
                                {resumeTimeFilters.map(filter => {
                                    const isSelected = filter.key === selectedTimeFilter.key;
                                    return (
                                        <button
                                            key={filter.key}
                                            className={`resume-filter-chip${isSelected ? ' selected' : ''}`}
                                            type="button"
                                            onClick={() => setSelectedTimeFilterKey(filter.key)}
                                            aria-pressed={isSelected}>
                                            {filter.label}
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Recent Employers</span>
                                <span className="resume-action-note">{selectedEmployerLimit.description}</span>
                            </div>
                            <div className="resume-filter-chip-list" role="group" aria-label="Resume employer filters">
                                {resumeEmployerLimits.map(limit => {
                                    const isSelected = limit.key === selectedEmployerLimit.key;
                                    return (
                                        <button
                                            key={limit.key}
                                            className={`resume-filter-chip${isSelected ? ' selected' : ''}`}
                                            type="button"
                                            onClick={() => setSelectedEmployerLimitKey(limit.key)}
                                            aria-pressed={isSelected}>
                                            {limit.label}
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Highlight Preview</span>
                                <span className="resume-action-note">Issue #87</span>
                            </div>
                            {hasSkillsSection ? (
                                <div className="resume-highlight-preview" aria-label="Resume highlight preview">
                                    {skillHighlights.slice(0, 3).map(highlight => (
                                        <span key={`skill-${highlight.label}`} className="resume-highlight-pill skill">
                                            <strong>{highlight.label}</strong>
                                            <span>{highlight.count} role{highlight.count === 1 ? '' : 's'}</span>
                                        </span>
                                    ))}
                                    {technologyHighlights.slice(0, 3).map(highlight => (
                                        <span key={`technology-${highlight.label}`} className="resume-highlight-pill technology">
                                            <strong>{highlight.label}</strong>
                                            <span>{highlight.count} role{highlight.count === 1 ? '' : 's'}</span>
                                        </span>
                                    ))}
                                </div>
                            ) : (
                                <p>Visible skills and technologies will promote here as resume data becomes available.</p>
                            )}
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">PDF Export</span>
                                <span className="resume-action-note">
                                    {canExportPdf
                                        ? 'Reflects the active resume filters.'
                                        : 'Available once resume data finishes loading.'}
                                </span>
                            </div>
                            <button
                                className="resume-action-button"
                                type="button"
                                disabled={!canExportPdf}
                                aria-disabled={!canExportPdf}
                                aria-label="Download PDF"
                                onClick={handleDownloadPdf}>
                                <span>Download PDF</span>
                                <span className="coming-soon-pill">{canExportPdf ? 'ATS-friendly' : 'Unavailable'}</span>
                            </button>
                        </div>
                        <div className="resume-action-card">
                            <div className="resume-action-copy">
                                <span className="meta-label">Resume Source</span>
                                <span className="resume-action-note">
                                    {resumeConfiguration?.isConfigured
                                        ? resumeSourceTypeLabel
                                        : isResumeConfigurationMissing || isProfileMissing
                                            ? 'Needs config'
                                            : 'Unavailable'}
                                </span>
                            </div>
                            {resumeConfiguration?.isConfigured && resumeConfiguration.sourceUrl && resumeConfiguration.displayLabel ? (
                                <a
                                    className="resume-action-button"
                                    href={resumeConfiguration.sourceUrl}
                                    target="_blank"
                                    rel="noreferrer"
                                    aria-label={resumeConfiguration.displayLabel}>
                                    <span>{resumeConfiguration.displayLabel}</span>
                                    <span className="coming-soon-pill">{resumeSourceTypeLabel}</span>
                                </a>
                            ) : (
                                <button
                                    className="resume-action-button"
                                    type="button"
                                    disabled
                                    aria-disabled="true"
                                    aria-label="Open Resume Source">
                                    <span>Open Resume Source</span>
                                    <span className="coming-soon-pill">Needs Config</span>
                                </button>
                            )}
                            {resumeConfiguration?.isConfigured && resumeConfiguration.summary ? (
                                <p>{resumeConfiguration.summary}</p>
                            ) : null}
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

                    {filteredEmployers.length > 0 ? (
                        <div className="resume-experience-list">
                            {filteredEmployers.map(employer => {
                                const employerLocation = getLocationLabel(employer.city, employer.region);
                                const employerDateValue = getEmployerDateValue(employer.name, employer.jobRoles);
                                const roleEntries = buildResumeRoleEntries(employer.jobRoles);

                                return (
                                    <section key={employer.id} className="resume-experience-group">
                                        <div className="resume-experience-header">
                                            <div>
                                                <h3>{employer.name}</h3>
                                                {employerLocation ? <p>{employerLocation}</p> : null}
                                            </div>
                                            {employerDateValue ? (
                                                <abbr className="resume-date-range" title={employerDateValue.title}>
                                                    {employerDateValue.label}
                                                </abbr>
                                            ) : null}
                                        </div>

                                        <div className="resume-experience-role-list">
                                            {roleEntries.map(({ jobRole, key, dateValue }: ResumeRoleEntry) => {
                                                const summary = getDescriptionSummary(jobRole.descriptionMarkdown);

                                                return (
                                                    <article
                                                        key={`${employer.id}-${key}`}
                                                        className="resume-experience-role">
                                                        <div className="resume-role-meta">
                                                            <div>
                                                                <strong>{jobRole.role}</strong>
                                                                <abbr className="resume-role-date" title={dateValue.title}>
                                                                    {dateValue.label}
                                                                </abbr>
                                                            </div>
                                                        </div>

                                                        {summary ? (
                                                            <p className="resume-role-copy">{summary}</p>
                                                        ) : null}

                                                        {jobRole.skills.length > 0 ? (
                                                            <div className="tag-group" aria-label={`${jobRole.role} skills`}>
                                                                {jobRole.skills.map(skill => (
                                                                    <span key={skill} className={`tag skill${highlightedSkillSet.has(skill) ? ' promoted' : ''}`}>{skill}</span>
                                                                ))}
                                                            </div>
                                                        ) : null}

                                                        {jobRole.technologies.length > 0 ? (
                                                            <div className="tag-group secondary" aria-label={`${jobRole.role} technologies`}>
                                                                {jobRole.technologies.map(technology => (
                                                                    <span key={technology} className={`tag technology${highlightedTechnologySet.has(technology) ? ' promoted' : ''}`}>{technology}</span>
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
                            {totalEmployerCount > 0
                                ? 'No published roles match the current resume filters. Expand the time window or increase the employer count to bring older experience back into view.'
                                : 'Published work history will appear here once employer and job-role records are available.'}
                        </p>
                    )}
                </article>

                {hasSkillsSection ? (
                    <article className="resume-panel">
                        <div className="resume-panel-heading">
                            <p className="eyebrow">Skills</p>
                            <h2>Highlighted skills and technologies.</h2>
                        </div>

                        <div className="resume-highlight-list">
                            {skillHighlights.length > 0 ? (
                                <section className="resume-highlight-group">
                                    <span className="meta-label">Skills</span>
                                    <div className="resume-highlight-grid" aria-label="Resume skills">
                                        {skillHighlights.map(skill => (
                                            <article key={skill.label} className="resume-highlight-card skill">
                                                <strong>{skill.label}</strong>
                                                <p>Appears in {skill.count} visible role{skill.count === 1 ? '' : 's'}.</p>
                                            </article>
                                        ))}
                                    </div>
                                </section>
                            ) : null}

                            {technologyHighlights.length > 0 ? (
                                <section className="resume-highlight-group">
                                    <span className="meta-label">Technologies</span>
                                    <div className="resume-highlight-grid" aria-label="Resume technologies">
                                        {technologyHighlights.map(technology => (
                                            <article key={technology.label} className="resume-highlight-card technology">
                                                <strong>{technology.label}</strong>
                                                <p>Appears in {technology.count} visible role{technology.count === 1 ? '' : 's'}.</p>
                                            </article>
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
