import profilePlaceholder from '../../assets/Placeholders/Profile-Placeholder.png';
import projectImageUnavailable from '../../assets/Placeholders/Project-Image-Unavailable.png';
import type { NavigateFn } from '../../app/navigation';
import {
    buildProjectsPath,
    formatFullDate,
    formatProjectDates,
    renderMarkdownParagraphs
} from '../../appSupport';
import { InternalLink } from '../../components/common/InternalLink';
import { MediaFrame } from '../../components/common/MediaFrame';
import { useProjectDetail } from '../../hooks/useProjectDetail';
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
    const { project, isLoading, error, isMissing } = useProjectDetail(projectId);
    const backPath = buildProjectsPath(listSearch);
    const completedMilestoneCount = project?.milestones.filter(milestone => !!milestone.completedOn).length ?? 0;
    const detailFacts = project
        ? [
            {
                label: 'Delivery Window',
                value: formatProjectDates(project.startDate, project.endDate),
                tone: 'default' as const
            },
            {
                label: 'Project Media',
                value: `${project.screenshots.length} screenshot${project.screenshots.length === 1 ? '' : 's'}`,
                tone: 'default' as const
            },
            {
                label: 'Collaborators',
                value: project.collaborators.length > 0
                    ? `${project.collaborators.length} teammate${project.collaborators.length === 1 ? '' : 's'}`
                    : 'Solo build',
                tone: 'default' as const
            },
            {
                label: 'Milestones',
                value: project.milestones.length > 0
                    ? `${completedMilestoneCount}/${project.milestones.length} completed`
                    : 'No milestones listed',
                tone: completedMilestoneCount > 0 ? 'success' as const : 'default' as const
            }
        ]
        : [];

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
                                    <div className="detail-heading-copy">
                                        <p className="project-dates">{formatProjectDates(project.startDate, project.endDate)}</p>
                                        <h1>{project.title}</h1>
                                    </div>
                                    {project.isFeatured ? <span className="featured-pill detail-featured-pill">Featured</span> : null}
                                </div>

                                <div className="detail-summary-card">
                                    <p className="detail-summary">{project.shortDescription}</p>

                                    {(project.demoUrl || project.gitHubUrl) ? (
                                        <div className="card-links detail-links">
                                            {project.demoUrl ? (
                                                <a className="primary-link" href={project.demoUrl} target="_blank" rel="noreferrer">
                                                    Live Demo
                                                </a>
                                            ) : null}
                                            {project.gitHubUrl ? (
                                                <a className="secondary-link" href={project.gitHubUrl} target="_blank" rel="noreferrer">
                                                    Source
                                                </a>
                                            ) : null}
                                        </div>
                                    ) : null}
                                </div>

                                <div className="detail-fact-grid" aria-label="Project overview facts">
                                    {detailFacts.map(fact => (
                                        <article key={fact.label} className={`detail-fact-card${fact.tone === 'success' ? ' success' : ''}`}>
                                            <span className="meta-label">{fact.label}</span>
                                            <strong>{fact.value}</strong>
                                        </article>
                                    ))}
                                </div>

                                {(project.developerRoles.length > 0 || project.skills.length > 0 || project.technologies.length > 0) ? (
                                    <section className="detail-stack-panel" aria-label="Project stack">
                                        <div className="detail-stack-header">
                                            <p className="eyebrow">Project Stack</p>
                                            <p className="secondary-copy">Roles, skills, and technologies that shaped the delivery.</p>
                                        </div>

                                        {project.developerRoles.length > 0 ? (
                                            <MetadataGroup label="Roles" items={project.developerRoles} tone="technology" />
                                        ) : null}

                                        {project.skills.length > 0 ? (
                                            <MetadataGroup label="Skills" items={project.skills} tone="skill" />
                                        ) : null}

                                        {project.technologies.length > 0 ? (
                                            <MetadataGroup label="Technologies" items={project.technologies} tone="technology" />
                                        ) : null}
                                    </section>
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
                                <div className="detail-visual-caption">
                                    <p className="eyebrow">Primary Preview</p>
                                    <p>{project.screenshots.length > 0 ? 'The screenshot gallery below expands on this project view.' : 'This project currently ships without an additional screenshot gallery.'}</p>
                                </div>
                            </div>
                        </section>

                        <section className="detail-grid">
                            <div className="detail-column detail-column-main">
                                {project.longDescriptionMarkdown.trim().length > 0 ? (
                                    <section className="detail-panel">
                                        <DetailPanelHeader
                                            eyebrow="Narrative"
                                            title="Overview"
                                            description="The delivery story, implementation context, and outcome for this project."
                                        />
                                        <div className="detail-markdown">
                                            {renderMarkdownParagraphs(project.longDescriptionMarkdown)}
                                        </div>
                                    </section>
                                ) : null}

                                {project.collaborators.length > 0 ? (
                                    <section className="detail-panel">
                                        <DetailPanelHeader
                                            eyebrow="Team"
                                            title="Collaborators"
                                            description="People who contributed alongside the primary build effort."
                                        />
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
                                        <DetailPanelHeader
                                            eyebrow="Delivery"
                                            title="Milestones"
                                            description="Planned and completed checkpoints that shaped the release."
                                        />
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
                                        <DetailPanelHeader
                                            eyebrow="Project Media"
                                            title="Screenshots"
                                            description="Select a frame to inspect the interface in more detail."
                                        />
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

interface DetailPanelHeaderProps {
    eyebrow: string;
    title: string;
    description: string;
}

function DetailPanelHeader({
    eyebrow,
    title,
    description
}: DetailPanelHeaderProps) {
    return (
        <header className="detail-panel-header">
            <p className="eyebrow">{eyebrow}</p>
            <div className="detail-panel-heading">
                <h2>{title}</h2>
                <p className="secondary-copy">{description}</p>
            </div>
        </header>
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
