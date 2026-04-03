import { startTransition, useDeferredValue, useEffect, useMemo, useRef, useState, type KeyboardEvent, type ReactNode, type TouchEvent } from 'react';
import profilePlaceholder from './assets/Placeholders/Profile-Placeholder.png';
import projectImageUnavailable from './assets/Placeholders/Project-Image-Unavailable.png';
import screenshotMissing from './assets/Placeholders/Screenshot-Missing.png';
import {
    buildDetailPath,
    buildListSearch,
    buildProjectsPath,
    createRouteKey,
    formatFullDate,
    formatProjectDates,
    mergeProjects,
    parseRoute,
    readLocation,
    renderMarkdownParagraphs,
    type AppLocation,
    type ListFilters
} from './appSupport';
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

interface ProjectScreenshot {
    imageUrl: string;
    caption?: string | null;
    sortOrder: number;
}

interface ProjectCollaborator {
    name: string;
    gitHubProfileUrl?: string | null;
    websiteUrl?: string | null;
    photoUrl?: string | null;
    roles: string[];
}

interface ProjectMilestone {
    title: string;
    targetDate: string;
    completedOn?: string | null;
    description?: string | null;
}

interface ProjectDetail {
    requestId?: string;
    id: number;
    title: string;
    startDate: string;
    endDate?: string | null;
    primaryImageUrl?: string | null;
    shortDescription: string;
    longDescriptionMarkdown: string;
    gitHubUrl?: string | null;
    demoUrl?: string | null;
    isPublished: boolean;
    isFeatured: boolean;
    screenshots: ProjectScreenshot[];
    developerRoles: string[];
    technologies: string[];
    skills: string[];
    collaborators: ProjectCollaborator[];
    milestones: ProjectMilestone[];
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

interface FeaturedProjectsResponse {
    requestId?: string;
    items: ProjectSummary[];
}

interface ApiErrorResponse {
    message?: string;
}

interface SiteShellContent {
    kicker: string;
    title: string;
    summary: string;
}

interface NavItem {
    label: string;
    description: string;
    href?: string;
}

type CarouselTransitionDirection = 'forward' | 'backward';

const pageSize = 6;
const startupRetryDelayMs = 2000;
const startupRetryAttempts = 2;
const startupRetryMessage = 'The portfolio API is still starting up. Please wait a moment and try again.';
const navItems: readonly NavItem[] = [
    { label: 'Home', href: '/', description: 'Featured highlights and introduction.' },
    { label: 'Projects', href: '/projects', description: 'Browse shipped work and project detail stories.' },
    { label: 'Timeline', description: 'Career milestones, education, and certifications.' },
    { label: 'About', description: 'Background, strengths, and developer story.' },
    { label: 'Resume', description: 'Resume hub and downloadable materials.' },
    { label: 'Contact', description: 'Direct outreach paths and social links.' },
    { label: 'Blog', description: 'Writing, updates, and thought pieces.' }
] as const;

class RetryableApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'RetryableApiError';
    }
}

function isRetryableApiError(error: unknown): error is RetryableApiError {
    return error instanceof RetryableApiError;
}

function isApiErrorResponse(payload: unknown): payload is ApiErrorResponse {
    return typeof payload === 'object'
        && payload !== null
        && 'message' in payload
        && (typeof (payload as ApiErrorResponse).message === 'string' || typeof (payload as ApiErrorResponse).message === 'undefined');
}

async function waitForRetry(delayMs: number, signal: AbortSignal) {
    await new Promise<void>((resolve, reject) => {
        const timeoutId = window.setTimeout(() => {
            signal.removeEventListener('abort', handleAbort);
            resolve();
        }, delayMs);

        const handleAbort = () => {
            window.clearTimeout(timeoutId);
            reject(new DOMException('The request was aborted.', 'AbortError'));
        };

        signal.addEventListener('abort', handleAbort, { once: true });
    });
}

async function readResponsePayload<TPayload>(response: Response) {
    const responseText = await response.text();

    if (responseText.trim().length === 0) {
        return null;
    }

    try {
        return JSON.parse(responseText) as TPayload | ApiErrorResponse;
    } catch {
        throw new RetryableApiError(startupRetryMessage);
    }
}

async function fetchJsonWithStartupRetry<TPayload>(
    input: string,
    init: RequestInit & { signal: AbortSignal },
    fallbackMessage: string
) {
    let attempt = 0;

    while (true) {
        try {
            const response = await fetch(input, init);
            const payload = await readResponsePayload<TPayload>(response);

            if (!response.ok) {
                const apiMessage = isApiErrorResponse(payload) ? payload.message : undefined;

                if (response.status >= 500) {
                    throw new RetryableApiError(apiMessage ?? startupRetryMessage);
                }

                throw new Error(apiMessage ?? fallbackMessage);
            }

            if (payload === null) {
                throw new RetryableApiError(startupRetryMessage);
            }

            return payload as TPayload;
        } catch (caughtError) {
            if ((caughtError as Error).name === 'AbortError') {
                throw caughtError;
            }

            if (!isRetryableApiError(caughtError) || attempt >= startupRetryAttempts) {
                throw caughtError instanceof Error ? caughtError : new Error(fallbackMessage);
            }

            attempt += 1;
            await waitForRetry(startupRetryDelayMs, init.signal);
        }
    }
}

function App() {
    const [location, setLocation] = useState<AppLocation>(() => readLocation());

    useEffect(() => {
        const handlePopState = () => {
            setLocation(readLocation());
        };

        window.addEventListener('popstate', handlePopState);
        return () => window.removeEventListener('popstate', handlePopState);
    }, []);

    const route = useMemo(() => parseRoute(location), [location]);
    const activeNavLabel = route.kind === 'home'
        ? 'Home'
        : route.kind === 'detail' || route.kind === 'list'
            ? 'Projects'
            : '';
    const shellContent = route.kind === 'home'
        ? {
            kicker: 'Project Portfolio',
            title: 'Home',
            summary: 'Start with a short introduction, then move through featured work before exploring the broader project archive.'
        } satisfies SiteShellContent
        : route.kind === 'detail'
        ? {
            kicker: 'Project Portfolio',
            title: 'Project Detail',
            summary: 'Review the project story, supporting media, collaborators, and milestone context.'
        } satisfies SiteShellContent
        : {
            kicker: 'Project Portfolio',
            title: 'Projects',
            summary: 'Browse shipped work, search the portfolio, and move into individual project case studies.'
        } satisfies SiteShellContent;

    function navigate(nextPath: string, options?: { replace?: boolean; preserveScroll?: boolean }) {
        const method = options?.replace ? 'replaceState' : 'pushState';
        window.history[method](window.history.state, '', nextPath);
        setLocation(readLocation());

        if (!options?.preserveScroll) {
            window.scrollTo({ top: 0, behavior: 'auto' });
        }
    }

    return (
        <SiteShell
            activeNavLabel={activeNavLabel}
            content={shellContent}
            onNavigate={navigate}>
            {route.kind === 'home' ? (
                <HomePage onNavigate={navigate} />
            ) : route.kind === 'detail' ? (
                <ProjectDetailPage
                    projectId={route.projectId}
                    listSearch={route.listSearch}
                    onNavigate={navigate} />
            ) : (
                <ProjectListPage
                    filters={route.filters}
                    onNavigate={navigate} />
            )}
        </SiteShell>
    );
}

function HomePage({
    onNavigate
}: {
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
}) {
    const [projects, setProjects] = useState<ProjectSummary[]>([]);
    const [activeIndex, setActiveIndex] = useState(0);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isPaused, setIsPaused] = useState(false);
    const [isMobile, setIsMobile] = useState(() => window.matchMedia('(max-width: 720px)').matches);
    const touchStartXRef = useRef<number | null>(null);

    useEffect(() => {
        const mediaQuery = window.matchMedia('(max-width: 720px)');
        const updateIsMobile = (event?: MediaQueryListEvent) => {
            setIsMobile(event?.matches ?? mediaQuery.matches);
        };

        updateIsMobile();
        mediaQuery.addEventListener('change', updateIsMobile);
        return () => mediaQuery.removeEventListener('change', updateIsMobile);
    }, []);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);

        void loadFeaturedProjects();

        return () => controller.abort();

        async function loadFeaturedProjects() {
            try {
                const featuredResponse = await fetchJsonWithStartupRetry<FeaturedProjectsResponse>(
                    `/api/projects/featured?limit=5&requestId=${requestId}`,
                    {
                        signal: controller.signal
                    },
                    'Unable to load featured projects right now.'
                );
                if (featuredResponse.requestId !== requestId) {
                    return;
                }

                setProjects(featuredResponse.items);
                setActiveIndex(0);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load featured projects right now.');
                setProjects([]);
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    useEffect(() => {
        if (isMobile || isPaused || projects.length <= 1) {
            return;
        }

        const timer = window.setInterval(() => {
            showNextProject();
        }, 5000);

        return () => window.clearInterval(timer);
    }, [isMobile, isPaused, projects.length]);

    useEffect(() => {
        if (activeIndex < projects.length) {
            return;
        }

        setActiveIndex(0);
    }, [activeIndex, projects.length]);

    function showPreviousProject() {
        if (projects.length <= 1) {
            return;
        }

        showRelativeProject(-1);
    }

    function showNextProject() {
        if (projects.length <= 1) {
            return;
        }

        showRelativeProject(1);
    }

    function showRelativeProject(step: number) {
        setActiveIndex(currentIndex => {
            const nextIndex = currentIndex + step;
            return (nextIndex + projects.length) % projects.length;
        });
    }

    function showProject(nextIndex: number, _direction: CarouselTransitionDirection) {
        if (nextIndex === activeIndex) {
            return;
        }

        setActiveIndex(nextIndex);
    }

    function handleCarouselKeyDown(event: KeyboardEvent<HTMLElement>) {
        if (isMobile || projects.length <= 1) {
            return;
        }

        if (event.key === 'ArrowLeft') {
            event.preventDefault();
            showPreviousProject();
        }

        if (event.key === 'ArrowRight') {
            event.preventDefault();
            showNextProject();
        }
    }

    function handleTouchStart(event: TouchEvent<HTMLElement>) {
        touchStartXRef.current = event.changedTouches[0]?.clientX ?? null;
    }

    function handleTouchEnd(event: TouchEvent<HTMLElement>) {
        const touchStartX = touchStartXRef.current;
        const touchEndX = event.changedTouches[0]?.clientX ?? null;

        touchStartXRef.current = null;

        if (touchStartX === null || touchEndX === null) {
            return;
        }

        const swipeDelta = touchStartX - touchEndX;
        if (Math.abs(swipeDelta) < 40 || projects.length <= 1) {
            return;
        }

        if (swipeDelta > 0) {
            showNextProject();
            return;
        }

        showPreviousProject();
    }

    const activeProject = projects[activeIndex] ?? null;

    return (
        <main className="home-page">
            <section className="home-intro">
                <p className="eyebrow">Developer Introduction</p>
                <div className="home-intro-grid">
                    <div className="home-intro-copy">
                        <h1>Building practical, resilient software that stays readable as products grow.</h1>
                        <p className="hero-description">
                            I design and ship full-stack applications with a bias toward maintainable architecture,
                            polished user experience, and calm, traceable delivery. This portfolio highlights projects
                            where product thinking and engineering discipline had to work together.
                        </p>
                    </div>

                    <div className="hero-stats" aria-label="Developer snapshot">
                        <div className="stat-card">
                            <span className="stat-label">Focus</span>
                            <strong>Full Stack</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Strengths</span>
                            <strong>UX + APIs</strong>
                        </div>
                        <div className="stat-card">
                            <span className="stat-label">Approach</span>
                            <strong>Ship Clearly</strong>
                        </div>
                    </div>
                </div>
            </section>

            <section className="featured-panel">
                <div className="featured-panel-heading">
                    <div>
                        <p className="eyebrow">Featured Projects</p>
                        <h2>Selected work worth opening first.</h2>
                    </div>
                    <InternalLink
                        className="secondary-link"
                        href="/projects"
                        onNavigate={onNavigate}>
                        Browse All Projects
                    </InternalLink>
                </div>

                {error ? <p className="status-banner error">{error}</p> : null}
                {!error && isLoading ? <p className="status-banner">Loading featured projects...</p> : null}
                {!error && !isLoading && !activeProject ? (
                    <p className="status-banner">Featured projects will appear here once published project data is available.</p>
                ) : null}

                {activeProject ? (
                    <section
                        className={`carousel-shell${isMobile ? ' mobile' : ''}`}
                        aria-label="Featured project carousel"
                        aria-roledescription="carousel"
                        tabIndex={0}
                        onKeyDown={handleCarouselKeyDown}
                        onMouseEnter={() => setIsPaused(true)}
                        onMouseLeave={() => setIsPaused(false)}
                        onTouchStart={handleTouchStart}
                        onTouchEnd={handleTouchEnd}>
                        <div className="carousel-track">
                            {projects.map((project, index) => (
                                <FeaturedCarouselSlide
                                    key={project.id}
                                    project={project}
                                    state={getFeaturedCardState(index, activeIndex, projects.length)}
                                    onNavigate={onNavigate}
                                />
                            ))}
                        </div>

                        {!isMobile && projects.length > 1 ? (
                            <>
                                <button
                                    className="carousel-arrow carousel-arrow-left"
                                    type="button"
                                    onClick={showPreviousProject}
                                    aria-label="Show previous featured project">
                                    {'<'}
                                </button>
                                <button
                                    className="carousel-arrow carousel-arrow-right"
                                    type="button"
                                    onClick={showNextProject}
                                    aria-label="Show next featured project">
                                    {'>'}
                                </button>
                            </>
                        ) : null}

                        <div className="carousel-indicators" aria-label="Featured project selection">
                            {projects.map((project, index) => (
                                <button
                                    key={project.id}
                                    className={`carousel-indicator${index === activeIndex ? ' active' : ''}`}
                                    type="button"
                                    onClick={() => showProject(index, index > activeIndex ? 'forward' : 'backward')}
                                    aria-label={`Show featured project ${index + 1}: ${project.title}`}
                                    aria-pressed={index === activeIndex}
                                />
                            ))}
                        </div>
                    </section>
                ) : null}
            </section>
        </main>
    );
}

function SiteShell({
    activeNavLabel,
    content,
    onNavigate,
    children
}: {
    activeNavLabel: string;
    content: SiteShellContent;
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
    children: ReactNode;
}) {
    return (
        <div className="site-shell">
            <aside className="site-sidebar" aria-label="Primary">
                <div className="site-brand">
                    <p className="brand-mark">PP26</p>
                    <div>
                        <strong>Project Portfolio</strong>
                        <p className="brand-copy">Built to scale as new public sections come online.</p>
                    </div>
                </div>

                <nav className="site-nav" aria-label="Primary site navigation">
                    {navItems.map(item => item.href ? (
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
                    ))}
                </nav>

                <p className="sidebar-footnote">
                    Resume will eventually include the Work History subsection.
                </p>
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

function FeaturedCarouselSlide({
    project,
    state,
    onNavigate
}: {
    project: ProjectSummary;
    state: 'active' | 'prev1' | 'prev2' | 'next1' | 'next2' | 'hidden';
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
}) {
    return (
        <article className={`carousel-slide ${state}`}>
            <MediaFrame
                src={project.primaryImageUrl}
                alt={`${project.title} featured preview`}
                fallbackLabel={project.title}
                fallbackSrc={projectImageUnavailable}
                className="carousel-background-media"
            />

            <div className="carousel-copy">
                <p className="project-dates">{formatProjectDates(project.startDate, project.endDate)}</p>
                <h3>{project.title}</h3>
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
                        href={buildDetailPath(project.id, '')}
                        onNavigate={onNavigate}>
                        View Project
                    </InternalLink>
                    <InternalLink
                        className="secondary-link"
                        href="/projects"
                        onNavigate={onNavigate}>
                        Open Archive
                    </InternalLink>
                </div>
            </div>
        </article>
    );
}

function getFeaturedCardState(index: number, activeIndex: number, total: number) {
    if (index === activeIndex) {
        return 'active';
    }

    if (index === (activeIndex - 1 + total) % total) {
        return 'prev1';
    }

    if (index === (activeIndex - 2 + total) % total) {
        return 'prev2';
    }

    if (index === (activeIndex + 1) % total) {
        return 'next1';
    }

    if (index === (activeIndex + 2) % total) {
        return 'next2';
    }

    return 'hidden';
}

function ProjectListPage({
    filters,
    onNavigate
}: {
    filters: ListFilters;
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
}) {
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
    const currentQueryKey = createRouteKey({
        searchInput,
        selectedSkills
    });
        const listSearch = buildListSearch({
        searchInput,
        selectedSkills
    });

    useEffect(() => {
        if (routeKey === currentQueryKey) {
            return;
        }

        setSearchInput(filters.searchInput);
        setSelectedSkills(filters.selectedSkills);
        setPage(1);
        setProjects([]);
        setAvailableSkills([]);
        setTotalCount(0);
        setHasMore(false);
        setError(null);
        setIsInitialLoad(true);
    }, [currentQueryKey, filters.searchInput, filters.selectedSkills, routeKey]);

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
        const isLoadingMore = page > 1 && previousQueryKeyRef.current === currentQueryKey;
        const queryKeyChanged = previousQueryKeyRef.current !== currentQueryKey;

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

                previousQueryKeyRef.current = currentQueryKey;
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

            const response = await fetch(`/api/projects?${query.toString()}`, {
                signal: controller.signal
            });
            const payload = await response.json() as ProjectListResponse | ApiErrorResponse;

            if (!response.ok) {
                const errorResponse = payload as ApiErrorResponse;
                throw new Error(errorResponse.message ?? 'Unable to load projects right now.');
            }

            const listResponse = payload as ProjectListResponse;

            if (listResponse.requestId !== requestId) {
                throw new DOMException('Stale response.', 'AbortError');
            }

            return listResponse;
        }
    }, [currentQueryKey, deferredSearch, page, selectedSkills]);

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
                {projects.map(project => (
                    <article key={project.id} className={`project-card${project.isFeatured ? ' featured' : ''}`}>
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
                ))}
            </section>

            <div ref={sentinelRef} className="scroll-sentinel" aria-hidden="true" />

            {isLoading && projects.length > 0 ? <p className="helper-copy">Loading more projects...</p> : null}
            {!hasMore && projects.length > 0 ? <p className="helper-copy">You&apos;ve reached the end of the published project list.</p> : null}
        </main>
    );
}

function ProjectDetailPage({
    projectId,
    listSearch,
    onNavigate
}: {
    projectId: number;
    listSearch: string;
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
}) {
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
                const response = await fetch(`/api/projects/${projectId}?${query.toString()}`, {
                    signal: controller.signal
                });

                if (response.status === 404) {
                    setIsMissing(true);
                    setProject(null);
                    return;
                }

                const payload = await response.json() as ProjectDetail | ApiErrorResponse;

                if (!response.ok) {
                    const errorResponse = payload as ApiErrorResponse;
                    throw new Error(errorResponse.message ?? 'Unable to load this project right now.');
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
    const screenshotGallery = project
        ? [project.primaryImageUrl, ...project.screenshots.map(screenshot => screenshot.imageUrl)]
            .filter((value, index, values): value is string => Boolean(value) && values.indexOf(value) === index)
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
                                    <div>
                                        <p className="project-dates">{formatProjectDates(project.startDate, project.endDate)}</p>
                                        <h1>{project.title}</h1>
                                    </div>
                                    {project.isFeatured ? <span className="featured-pill detail-featured-pill">Featured</span> : null}
                                </div>

                                <p className="detail-summary">{project.shortDescription}</p>

                                {project.developerRoles.length > 0 ? (
                                    <div className="detail-meta">
                                        <span className="meta-label">Roles</span>
                                        <div className="tag-group">
                                            {project.developerRoles.map(role => (
                                                <span key={role} className="tag technology">{role}</span>
                                            ))}
                                        </div>
                                    </div>
                                ) : null}

                                {project.skills.length > 0 ? (
                                    <div className="detail-meta">
                                        <span className="meta-label">Skills</span>
                                        <div className="tag-group">
                                            {project.skills.map(skill => (
                                                <span key={skill} className="tag skill">{skill}</span>
                                            ))}
                                        </div>
                                    </div>
                                ) : null}

                                {project.technologies.length > 0 ? (
                                    <div className="detail-meta">
                                        <span className="meta-label">Technologies</span>
                                        <div className="tag-group">
                                            {project.technologies.map(technology => (
                                                <span key={technology} className="tag technology">{technology}</span>
                                            ))}
                                        </div>
                                    </div>
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
                            {project.longDescriptionMarkdown.trim().length > 0 ? (
                                <section className="detail-panel detail-panel-wide">
                                    <h2>Overview</h2>
                                    <div className="detail-markdown">
                                        {renderMarkdownParagraphs(project.longDescriptionMarkdown)}
                                    </div>
                                </section>
                            ) : null}

                            {project.screenshots.length > 0 ? (
                                <section className="detail-panel detail-panel-wide">
                                    <h2>Screenshots</h2>
                                    <div className="screenshot-grid">
                                        {project.screenshots.map(screenshot => (
                                            <figure key={`${screenshot.sortOrder}-${screenshot.imageUrl}`} className="shot-card">
                                                <MediaFrame
                                                    src={screenshot.imageUrl}
                                                    alt={screenshot.caption?.trim() || `${project.title} screenshot ${screenshot.sortOrder}`}
                                                    fallbackLabel={screenshot.caption?.trim() || `${project.title} ${screenshot.sortOrder}`}
                                                    fallbackSrc={screenshotMissing}
                                                    className="shot-media"
                                                />
                                                {screenshot.caption?.trim() ? <figcaption>{screenshot.caption}</figcaption> : null}
                                            </figure>
                                        ))}
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

                            {screenshotGallery.length > 0 ? (
                                <section className="detail-panel detail-panel-wide">
                                    <h2>Gallery Rail</h2>
                                    <div className="gallery-rail" aria-label={`${project.title} image gallery`}>
                                        {screenshotGallery.map(imageUrl => (
                                            <MediaFrame
                                                key={imageUrl}
                                                src={imageUrl}
                                                alt={`${project.title} gallery image`}
                                                fallbackLabel={project.title}
                                                fallbackSrc={screenshotMissing}
                                                className="gallery-media"
                                            />
                                        ))}
                                    </div>
                                </section>
                            ) : null}
                        </section>
                    </>
                ) : null}
            </section>
        </main>
    );
}

function InternalLink({
    className,
    href,
    ariaCurrent,
    onNavigate,
    children,
    preserveScroll
}: {
    className?: string;
    href: string;
    ariaCurrent?: 'page';
    onNavigate: (path: string, options?: { replace?: boolean; preserveScroll?: boolean }) => void;
    children: string;
    preserveScroll?: boolean;
}) {
    return (
        <a
            className={className}
            href={href}
            aria-current={ariaCurrent}
            onClick={event => {
                event.preventDefault();
                onNavigate(href, { preserveScroll });
            }}>
            {children}
        </a>
    );
}

function MediaFrame({
    src,
    alt,
    fallbackLabel,
    fallbackSrc,
    className,
    compact = false
}: {
    src?: string | null;
    alt: string;
    fallbackLabel: string;
    fallbackSrc?: string;
    className?: string;
    compact?: boolean;
}) {
    const [failedSource, setFailedSource] = useState<string | null>(null);
    const hasFailedPrimary = !!src && failedSource === src;
    const hasFailedFallback = !!fallbackSrc && failedSource === fallbackSrc;
    const candidateSrc = (!src || hasFailedPrimary)
        ? (hasFailedFallback ? undefined : fallbackSrc)
        : src;

    if (!candidateSrc) {
        return (
            <div className={`${className ?? ''} media-fallback${compact ? ' compact' : ''}`.trim()}>
                <span>{fallbackLabel.slice(0, compact ? 2 : 3).toUpperCase()}</span>
            </div>
        );
    }

    return (
        <img
            className={className}
            src={candidateSrc}
            alt={alt}
            loading="lazy"
            onError={() => setFailedSource(candidateSrc)}
        />
    );
}

export default App;
