import { Fragment } from 'react';
import projectImageUnavailable from '../../assets/Placeholders/Project-Image-Unavailable.png';
import type { NavigateFn } from '../../app/navigation';
import type { ProjectSummary } from '../../app/types';
import {
    buildDetailPath,
    formatProjectDates,
    getProjectYearSpacerLabel,
    type ListFilters
} from '../../appSupport';
import { InternalLink } from '../../components/common/InternalLink';
import { MediaFrame } from '../../components/common/MediaFrame';
import { useProjectList } from '../../hooks/useProjectList';

interface ProjectListPageProps {
    filters: ListFilters;
    onNavigate: NavigateFn;
}

export function ProjectListPage({
    filters,
    onNavigate
}: ProjectListPageProps) {
    const {
        searchInput,
        selectedSkills,
        projects,
        availableSkills,
        totalCount,
        hasMore,
        isInitialLoad,
        isLoading,
        error,
        listSearch,
        sentinelRef,
        handleSearchChange,
        toggleSkill,
        clearFilters
    } = useProjectList({
        filters,
        onNavigate
    });

    const showingCount = projects.length;
    const activeFilterCount = selectedSkills.length + (searchInput.trim().length > 0 ? 1 : 0);
    const isEmpty = !isLoading && !error && showingCount === 0;

    return (
        <main className="portfolio-page">
            <section className="hero-panel">
                <p className="eyebrow">Project Atlas</p>
                <div className="hero-copy">
                    <div>
                        <h1>Browse shipped work, then dive into the story behind each release.</h1>
                        <p className="hero-description">
                            Search the portfolio, narrow by skill tags, and open a dedicated project view without
                            losing the active filter set that got you there.
                        </p>
                    </div>

                    <div className="hero-stats" aria-label="Project list summary">
                        <div className="stat-card">
                            <span className="stat-label">Published Projects</span>
                            <strong>{totalCount}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Visible Cards</span>
                            <strong>{showingCount}</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Active Filters</span>
                            <strong>{activeFilterCount}</strong>
                        </div>
                    </div>
                </div>
            </section>

            <section className="sticky-filters">
                <div className="toolbar" aria-label="Project filters">
                    <label className="search-panel" htmlFor="project-search">
                        <span className="search-label">Search projects</span>
                        <input
                            id="project-search"
                            name="project-search"
                            type="search"
                            value={searchInput}
                            onChange={event => handleSearchChange(event.target.value)}
                            placeholder="Search by title, summary, technology, or skill"
                        />
                    </label>

                    <button
                        className="clear-button"
                        type="button"
                        onClick={clearFilters}
                        disabled={searchInput.length === 0 && selectedSkills.length === 0}>
                        Reset filters
                    </button>
                </div>

                <section className="skill-strip" aria-label="Skill filters">
                    {availableSkills.length === 0 && isInitialLoad ? (
                        <p className="helper-copy">Loading skill filters...</p>
                    ) : availableSkills.length === 0 ? (
                        <p className="helper-copy">Skill filters will appear once published projects are available.</p>
                    ) : (
                        availableSkills.map(skill => {
                            const isSelected = selectedSkills.includes(skill);

                            return (
                                <button
                                    key={skill}
                                    className={`skill-chip${isSelected ? ' selected' : ''}`}
                                    type="button"
                                    onClick={() => toggleSkill(skill)}
                                    aria-pressed={isSelected}>
                                    {skill}
                                </button>
                            );
                        })
                    )}
                </section>
            </section>

            {error ? <p className="status-banner error">{error}</p> : null}
            {!error && isLoading && projects.length === 0 ? <p className="status-banner">Loading published projects...</p> : null}
            {isEmpty ? <p className="status-banner">No published projects matched the current search and skill filters.</p> : null}

            <section className="project-list" aria-label="Project results">
                {projects.map((project, index) => (
                    <ProjectCard
                        key={project.id}
                        project={project}
                        index={index}
                        allProjects={projects}
                        listSearch={listSearch}
                        onNavigate={onNavigate}
                    />
                ))}
            </section>

            <div ref={sentinelRef} className="scroll-sentinel" aria-hidden="true" />

            {isLoading && projects.length > 0 ? <p className="helper-copy">Loading more projects...</p> : null}
            {!hasMore && projects.length > 0 ? <p className="helper-copy">You&apos;ve reached the end of the published project list.</p> : null}
        </main>
    );
}

interface ProjectCardProps {
    project: ProjectSummary;
    index: number;
    allProjects: ProjectSummary[];
    listSearch: string;
    onNavigate: NavigateFn;
}

function ProjectCard({
    project,
    index,
    allProjects,
    listSearch,
    onNavigate
}: ProjectCardProps) {
    const yearLabel = getProjectYearSpacerLabel(project.endDate);
    const previousYearLabel = index > 0
        ? getProjectYearSpacerLabel(allProjects[index - 1].endDate)
        : null;
    const shouldRenderSpacer = yearLabel !== previousYearLabel;

    return (
        <Fragment>
            {shouldRenderSpacer ? (
                <div className="project-year-spacer" aria-label={`Projects ending in ${yearLabel}`}>
                    <span className="project-year-rule" aria-hidden="true" />
                    <p className="eyebrow">{yearLabel}</p>
                    <span className="project-year-rule" aria-hidden="true" />
                </div>
            ) : null}

            <article className={`project-card${project.isFeatured ? ' featured' : ''}`}>
                <div className="image-shell">
                    <MediaFrame
                        src={project.primaryImageUrl}
                        alt={`${project.title} cover art`}
                        fallbackLabel={project.title}
                        fallbackSrc={projectImageUnavailable}
                    />
                    {project.isFeatured ? <span className="featured-pill">Featured</span> : null}
                </div>

                <div className="card-copy">
                    <div className="card-heading">
                        <p className="project-dates">{formatProjectDates(project.startDate, project.endDate)}</p>
                        <h2>{project.title}</h2>
                    </div>

                    <p className="project-summary">{project.shortDescription}</p>

                    <div className="tag-group" aria-label={`${project.title} skills`}>
                        {project.skills.map(skill => (
                            <span key={skill} className="tag skill">{skill}</span>
                        ))}
                    </div>

                    <div className="tag-group secondary" aria-label={`${project.title} technologies`}>
                        {project.technologies.map(technology => (
                            <span key={technology} className="tag technology">{technology}</span>
                        ))}
                    </div>

                    <div className="card-links">
                        <InternalLink
                            className="primary-link"
                            href={buildDetailPath(project.id, listSearch)}
                            onNavigate={onNavigate}>
                            View Details
                        </InternalLink>
                    </div>
                </div>
            </article>
        </Fragment>
    );
}
