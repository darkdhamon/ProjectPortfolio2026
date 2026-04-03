import { startTransition, useDeferredValue, useEffect, useRef, useState } from 'react';
import './App.css';

interface ProjectSummary {
    requestId?: string;
    id: number;
    title: string;
    startDate: string;
    endDate?: string | null;
    primaryImageUrl?: string | null;
    shortDescription: string;
    gitHubUrl?: string | null;
    demoUrl?: string | null;
    isFeatured: boolean;
    skills: string[];
    technologies: string[];
}

interface ProjectListResponse {
    requestId?: string;
    items: ProjectSummary[];
    page: number;
    pageSize: number;
    totalCount: number;
    hasMore: boolean;
    availableSkills: string[];
}

interface ApiErrorResponse {
    message?: string;
}

const pageSize = 6;

function App() {
    const [searchInput, setSearchInput] = useState('');
    const deferredSearch = useDeferredValue(searchInput.trim());
    const [selectedSkills, setSelectedSkills] = useState<string[]>([]);
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

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();
        const isLoadingMore = page > 1;

        latestRequestIdRef.current = requestId;
        setIsLoading(true);
        setError(null);

        const query = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString(),
            requestId
        });

        if (deferredSearch.length > 0) {
            query.set('search', deferredSearch);
        }

        if (selectedSkills.length > 0) {
            query.set('skills', selectedSkills.join(','));
        }

        void loadProjects();

        return () => {
            controller.abort();
        };

        async function loadProjects() {
            try {
                const response = await fetch(`/api/projects?${query.toString()}`, {
                    signal: controller.signal
                });
                const payload = await response.json() as ProjectListResponse | ApiErrorResponse;

                if (latestRequestIdRef.current !== requestId) {
                    return;
                }

                if (!response.ok) {
                    const errorResponse = payload as ApiErrorResponse;
                    throw new Error(errorResponse.message ?? 'Unable to load projects right now.');
                }

                const listResponse = payload as ProjectListResponse;

                if (listResponse.requestId !== requestId) {
                    return;
                }

                startTransition(() => {
                    setProjects(currentProjects => isLoadingMore
                        ? mergeProjects(currentProjects, listResponse.items)
                        : listResponse.items);
                    setAvailableSkills(listResponse.availableSkills);
                    setTotalCount(listResponse.totalCount);
                    setHasMore(listResponse.hasMore);
                });
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
    }, [deferredSearch, page, selectedSkills]);

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
                        <h1>Browse shipped work without losing your place.</h1>
                        <p className="hero-description">
                            Search the portfolio, narrow by skill tags, and keep scrolling through published work.
                            The client tracks request IDs so late responses never overwrite newer filters.
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
                {projects.map(project => (
                    <article key={project.id} className={`project-card${project.isFeatured ? ' featured' : ''}`}>
                        <div className="image-shell">
                            {project.primaryImageUrl ? (
                                <img src={project.primaryImageUrl} alt="" loading="lazy" />
                            ) : (
                                <div className="image-fallback">
                                    <span>{project.title.slice(0, 2).toUpperCase()}</span>
                                </div>
                            )}
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
                        </div>
                    </article>
                ))}
            </section>

            <div ref={sentinelRef} className="scroll-sentinel" aria-hidden="true" />

            {isLoading && projects.length > 0 ? <p className="helper-copy">Loading more projects...</p> : null}
            {!hasMore && projects.length > 0 ? <p className="helper-copy">You&apos;ve reached the end of the published project list.</p> : null}
        </main>
    );
}

function mergeProjects(currentProjects: ProjectSummary[], nextProjects: ProjectSummary[]) {
    const seen = new Set(currentProjects.map(project => project.id));
    const mergedProjects = [...currentProjects];

    for (const project of nextProjects) {
        if (!seen.has(project.id)) {
            mergedProjects.push(project);
            seen.add(project.id);
        }
    }

    return mergedProjects;
}

function formatProjectDates(startDate: string, endDate?: string | null) {
    const start = formatMonth(startDate);
    const end = endDate ? formatMonth(endDate) : 'Present';
    return `${start} to ${end}`;
}

function formatMonth(value: string) {
    return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
        month: 'short',
        year: 'numeric'
    });
}

export default App;
