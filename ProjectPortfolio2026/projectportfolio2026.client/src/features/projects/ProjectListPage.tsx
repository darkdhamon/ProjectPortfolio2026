import { Fragment, startTransition, useDeferredValue, useEffect, useRef, useState } from 'react';
import projectImageUnavailable from '../../assets/Placeholders/Project-Image-Unavailable.png';
import { fetchJsonWithStartupRetry } from '../../app/api';
import type { NavigateFn } from '../../app/navigation';
import type { ProjectListResponse, ProjectSummary } from '../../app/types';
import {
    buildDetailPath,
    buildListSearch,
    buildProjectsPath,
    createRouteKey,
    formatProjectDates,
    getProjectYearSpacerLabel,
    mergeProjects,
    type ListFilters
} from '../../appSupport';
import { InternalLink } from '../../components/common/InternalLink';
import { MediaFrame } from '../../components/common/MediaFrame';

const pageSize = 6;

interface ProjectListPageProps {
    filters: ListFilters;
    onNavigate: NavigateFn;
}

export function ProjectListPage({
    filters,
    onNavigate
}: ProjectListPageProps) {
    const [searchInput, setSearchInput] = useState(filters.searchInput);
    const deferredSearch = useDeferredValue(searchInput.trim());
    const [selectedSkills, setSelectedSkills] = useState<string[]>(filters.selectedSkills);
    const [projects, setProjects] = useState<ProjectSummary[]>([]);
    const [availableSkills, setAvailableSkills] = useState<string[]>([]);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [hasMore, setHasMore] = useState(false);
    const [isInitialLoad, setIsInitialLoad] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const latestRequestIdRef = useRef<string | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);
    const previousQueryKeyRef = useRef<string | null>(null);
    const routeKey = createRouteKey(filters);
    const previousRouteKeyRef = useRef(routeKey);
    const fetchQueryKey = createRouteKey({
        searchInput: deferredSearch,
        selectedSkills
    });
    const listSearch = buildListSearch({
        searchInput,
        selectedSkills
    });

    useEffect(() => {
        if (previousRouteKeyRef.current === routeKey) {
            return;
        }

        previousRouteKeyRef.current = routeKey;
        setSearchInput(filters.searchInput);
        setSelectedSkills(filters.selectedSkills);
        setPage(1);
        setProjects([]);
        setAvailableSkills([]);
        setTotalCount(0);
        setHasMore(false);
        setError(null);
        setIsInitialLoad(true);
    }, [filters.searchInput, filters.selectedSkills, routeKey]);

    useEffect(() => {
        const nextPath = buildProjectsPath(listSearch);
        const currentPath = `${window.location.pathname}${window.location.search}`;

        if (currentPath !== nextPath) {
            onNavigate(nextPath, { replace: true, preserveScroll: true });
        }
    }, [listSearch, onNavigate]);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();
        const isLoadingMore = page > 1 && previousQueryKeyRef.current === fetchQueryKey;
        const queryKeyChanged = previousQueryKeyRef.current !== fetchQueryKey;

        latestRequestIdRef.current = requestId;
        setIsLoading(true);
        setError(null);

        void loadProjects();

        return () => controller.abort();

        async function loadProjects() {
            try {
                const responses = queryKeyChanged
                    ? await loadPages(1, page)
                    : [await loadSinglePage(page)];

                if (latestRequestIdRef.current !== requestId || responses.length === 0) {
                    return;
                }

                startTransition(() => {
                    const latestResponse = responses[responses.length - 1];
                    const incomingProjects = responses.flatMap(response => response.items);

                    setProjects(currentProjects => isLoadingMore
                        ? mergeProjects(currentProjects, incomingProjects)
                        : mergeProjects([], incomingProjects));
                    setAvailableSkills(latestResponse.availableSkills);
                    setTotalCount(latestResponse.totalCount);
                    setHasMore(latestResponse.hasMore);
                });

                previousQueryKeyRef.current = fetchQueryKey;
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError' || latestRequestIdRef.current !== requestId) {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load projects right now.');
                if (!isLoadingMore) {
                    setProjects([]);
                    setAvailableSkills([]);
                    setTotalCount(0);
                    setHasMore(false);
                }
            } finally {
                if (latestRequestIdRef.current === requestId) {
                    setIsInitialLoad(false);
                    setIsLoading(false);
                }
            }
        }

        async function loadPages(startPage: number, endPage: number) {
            const pages: ProjectListResponse[] = [];

            for (let currentPage = startPage; currentPage <= endPage; currentPage += 1) {
                const response = await loadSinglePage(currentPage);
                pages.push(response);
            }

            return pages;
        }

        async function loadSinglePage(currentPage: number) {
            const query = new URLSearchParams({
                page: currentPage.toString(),
                pageSize: pageSize.toString(),
                requestId
            });

            if (deferredSearch.length > 0) {
                query.set('search', deferredSearch);
            }

            if (selectedSkills.length > 0) {
                query.set('skills', selectedSkills.join(','));
            }

            const listResponse = await fetchJsonWithStartupRetry<ProjectListResponse>(
                `/api/projects?${query.toString()}`,
                {
                    signal: controller.signal
                },
                'Unable to load projects right now.'
            );

            if (listResponse.requestId !== requestId) {
                throw new DOMException('Stale response.', 'AbortError');
            }

            return listResponse;
        }
    }, [deferredSearch, fetchQueryKey, page, selectedSkills]);

    useEffect(() => {
        const sentinel = sentinelRef.current;
        if (!sentinel || !hasMore || isLoading) {
            return;
        }

        const observer = new IntersectionObserver(entries => {
            if (entries.some(entry => entry.isIntersecting)) {
                setPage(currentPage => currentPage + 1);
            }
        }, { rootMargin: '240px 0px' });

        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [hasMore, isLoading, projects.length]);

    function handleSearchChange(nextValue: string) {
        setSearchInput(nextValue);
        setPage(1);
    }

    function toggleSkill(skill: string) {
        setSelectedSkills(currentSkills =>
            currentSkills.includes(skill)
                ? currentSkills.filter(currentSkill => currentSkill !== skill)
                : [...currentSkills, skill]);
        setPage(1);
    }

    function clearFilters() {
        setSearchInput('');
        setSelectedSkills([]);
        setPage(1);
    }

    const showingCount = projects.length;
    const isEmpty = !isLoading && !error && projects.length === 0;

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
                            <strong>{selectedSkills.length + (deferredSearch.length > 0 ? 1 : 0)}</strong>
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
