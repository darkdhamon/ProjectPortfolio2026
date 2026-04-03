import { startTransition, useEffect, useState } from 'react';
import profilePlaceholder from '../../assets/Placeholders/Profile-Placeholder.png';
import projectImageUnavailable from '../../assets/Placeholders/Project-Image-Unavailable.png';
import {
    fetchResponsePayloadWithStartupRetry,
    RetryableApiError,
    startupRetryMessage
} from '../../app/api';
import type { NavigateFn } from '../../app/navigation';
import type { ProjectDetail } from '../../app/types';
import {
    buildProjectsPath,
    formatFullDate,
    formatProjectDates,
    renderMarkdownParagraphs
} from '../../appSupport';
import { InternalLink } from '../../components/common/InternalLink';
import { MediaFrame } from '../../components/common/MediaFrame';
import { ScreenshotCarousel } from '../media/ScreenshotCarousel';

interface ProjectDetailPageProps {
    projectId: number;
    listSearch: string;
    onNavigate: NavigateFn;
}

export function ProjectDetailPage({
    projectId,
    listSearch,
    onNavigate
}: ProjectDetailPageProps) {
    const [project, setProject] = useState<ProjectDetail | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isMissing, setIsMissing] = useState(false);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);
        setIsMissing(false);

        void loadProject();

        return () => controller.abort();

        async function loadProject() {
            try {
                const query = new URLSearchParams({ requestId });
                const { response, payload } = await fetchResponsePayloadWithStartupRetry<ProjectDetail>(
                    `/api/projects/${projectId}?${query.toString()}`,
                    {
                        signal: controller.signal
                    },
                    'Unable to load this project right now.',
                    [404]
                );

                if (response.status === 404) {
                    setIsMissing(true);
                    setProject(null);
                    return;
                }

                if (payload === null) {
                    throw new RetryableApiError(startupRetryMessage);
                }

                const projectResponse = payload as ProjectDetail;

                if (projectResponse.requestId !== requestId) {
                    return;
                }

                startTransition(() => setProject(projectResponse));
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load this project right now.');
                setProject(null);
            } finally {
                setIsLoading(false);
            }
        }
    }, [projectId]);

    const backPath = buildProjectsPath(listSearch);

    return (
        <main className="portfolio-page detail-page">
            <section className="detail-shell">
                <div className="detail-nav">
                    <InternalLink
                        className="back-link"
                        href={backPath}
                        onNavigate={onNavigate}
                        preserveScroll>
                        Back to project list
                    </InternalLink>
                </div>

                {isLoading ? <p className="status-banner">Loading project details...</p> : null}
                {error ? <p className="status-banner error">{error}</p> : null}
                {isMissing ? <p className="status-banner">That project could not be found or is no longer public.</p> : null}

                {project ? (
                    <>
                        <section className="detail-hero">
                            <div className="detail-copy">
                                <p className="eyebrow">Project Detail</p>
                                <div className="detail-heading">
                                    <div>
                                        <p className="project-dates">{formatProjectDates(project.startDate, project.endDate)}</p>
                                        <h1>{project.title}</h1>
                                    </div>
                                    {project.isFeatured ? <span className="featured-pill detail-featured-pill">Featured</span> : null}
                                </div>

                                <p className="detail-summary">{project.shortDescription}</p>

                                {project.developerRoles.length > 0 ? (
                                    <MetadataGroup label="Roles" items={project.developerRoles} tone="technology" />
                                ) : null}

                                {project.skills.length > 0 ? (
                                    <MetadataGroup label="Skills" items={project.skills} tone="skill" />
                                ) : null}

                                {project.technologies.length > 0 ? (
                                    <MetadataGroup label="Technologies" items={project.technologies} tone="technology" />
                                ) : null}

                                {(project.demoUrl || project.gitHubUrl) ? (
                                    <div className="card-links detail-links">
                                        {project.demoUrl ? (
                                            <a href={project.demoUrl} target="_blank" rel="noreferrer">
                                                Live Demo
                                            </a>
                                        ) : null}
                                        {project.gitHubUrl ? (
                                            <a href={project.gitHubUrl} target="_blank" rel="noreferrer">
                                                Source
                                            </a>
                                        ) : null}
                                    </div>
                                ) : null}
                            </div>

                            <div className="detail-visual">
                                <MediaFrame
                                    src={project.primaryImageUrl}
                                    alt={`${project.title} hero screenshot`}
                                    fallbackLabel={project.title}
                                    fallbackSrc={projectImageUnavailable}
                                    className="detail-media"
                                />
                            </div>
                        </section>

                        <section className="detail-grid">
                            <div className="detail-column detail-column-main">
                                {project.longDescriptionMarkdown.trim().length > 0 ? (
                                    <section className="detail-panel">
                                        <h2>Overview</h2>
                                        <div className="detail-markdown">
                                            {renderMarkdownParagraphs(project.longDescriptionMarkdown)}
                                        </div>
                                    </section>
                                ) : null}

                                {project.collaborators.length > 0 ? (
                                    <section className="detail-panel">
                                        <h2>Collaborators</h2>
                                        <div className="stack-list">
                                            {project.collaborators.map(collaborator => (
                                                <article key={collaborator.name} className="stack-card collaborator-card">
                                                    <div className="collaborator-header">
                                                        <MediaFrame
                                                            src={collaborator.photoUrl}
                                                            alt={`${collaborator.name} profile`}
                                                            fallbackLabel={collaborator.name}
                                                            fallbackSrc={profilePlaceholder}
                                                            className="collaborator-photo"
                                                            compact
                                                        />
                                                        <div>
                                                            <h3>{collaborator.name}</h3>
                                                            {collaborator.roles.length > 0 ? (
                                                                <p className="secondary-copy">{collaborator.roles.join(' | ')}</p>
                                                            ) : null}
                                                        </div>
                                                    </div>

                                                    {(collaborator.gitHubProfileUrl || collaborator.websiteUrl) ? (
                                                        <div className="inline-links">
                                                            {collaborator.gitHubProfileUrl ? (
                                                                <a href={collaborator.gitHubProfileUrl} target="_blank" rel="noreferrer">
                                                                    GitHub
                                                                </a>
                                                            ) : null}
                                                            {collaborator.websiteUrl ? (
                                                                <a href={collaborator.websiteUrl} target="_blank" rel="noreferrer">
                                                                    Website
                                                                </a>
                                                            ) : null}
                                                        </div>
                                                    ) : null}
                                                </article>
                                            ))}
                                        </div>
                                    </section>
                                ) : null}

                                {project.milestones.length > 0 ? (
                                    <section className="detail-panel">
                                        <h2>Milestones</h2>
                                        <div className="stack-list">
                                            {project.milestones.map(milestone => (
                                                <article key={`${milestone.title}-${milestone.targetDate}`} className="stack-card">
                                                    <div className="milestone-heading">
                                                        <div>
                                                            <h3>{milestone.title}</h3>
                                                            <p className="secondary-copy">
                                                                Target: {formatFullDate(milestone.targetDate)}
                                                                {milestone.completedOn ? ` | Completed ${formatFullDate(milestone.completedOn)}` : ''}
                                                            </p>
                                                        </div>
                                                        <span className={`milestone-pill${milestone.completedOn ? ' completed' : ''}`}>
                                                            {milestone.completedOn ? 'Completed' : 'Planned'}
                                                        </span>
                                                    </div>
                                                    {milestone.description?.trim() ? <p>{milestone.description}</p> : null}
                                                </article>
                                            ))}
                                        </div>
                                    </section>
                                ) : null}
                            </div>

                            <div className="detail-column detail-column-media">
                                {project.screenshots.length > 0 ? (
                                    <section className="detail-panel">
                                        <h2>Screenshots</h2>
                                        <ScreenshotCarousel
                                            projectTitle={project.title}
                                            screenshots={project.screenshots}
                                        />
                                    </section>
                                ) : null}
                            </div>
                        </section>
                    </>
                ) : null}
            </section>
        </main>
    );
}

interface MetadataGroupProps {
    label: string;
    items: string[];
    tone: 'skill' | 'technology';
}

function MetadataGroup({
    label,
    items,
    tone
}: MetadataGroupProps) {
    return (
        <div className="detail-meta">
            <span className="meta-label">{label}</span>
            <div className="tag-group">
                {items.map(item => (
                    <span key={item} className={`tag ${tone}`}>{item}</span>
                ))}
            </div>
        </div>
    );
}
