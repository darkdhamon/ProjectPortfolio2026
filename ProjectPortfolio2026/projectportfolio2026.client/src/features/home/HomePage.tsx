import { useEffect, useEffectEvent, useRef, useState, type KeyboardEvent, type TouchEvent } from 'react';
import projectImageUnavailable from '../../assets/Placeholders/Project-Image-Unavailable.png';
import { fetchJsonWithStartupRetry } from '../../app/api';
import type { NavigateFn } from '../../app/navigation';
import type { FeaturedProjectsResponse, ProjectSummary } from '../../app/types';
import { buildDetailPath, formatProjectDates } from '../../appSupport';
import { InternalLink } from '../../components/common/InternalLink';
import { MediaFrame } from '../../components/common/MediaFrame';

interface HomePageProps {
    onNavigate: NavigateFn;
}

export function HomePage({ onNavigate }: HomePageProps) {
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
            handleAutoplayAdvance();
        }, 5000);

        return () => window.clearInterval(timer);
    }, [isMobile, isPaused, projects.length]);

    useEffect(() => {
        if (activeIndex < projects.length) {
            return;
        }

        setActiveIndex(0);
    }, [activeIndex, projects.length]);

    function showRelativeProject(step: number) {
        setActiveIndex(currentIndex => {
            const nextIndex = currentIndex + step;
            return (nextIndex + projects.length) % projects.length;
        });
    }

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

    function showProject(nextIndex: number) {
        if (nextIndex !== activeIndex) {
            setActiveIndex(nextIndex);
        }
    }

    const handleAutoplayAdvance = useEffectEvent(() => {
        setActiveIndex(currentIndex => (currentIndex + 1) % projects.length);
    });

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
                    <div className="featured-carousel-block">
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
                        </section>

                        <div className="carousel-indicators" aria-label="Featured project selection">
                            {projects.map((project, index) => (
                                <button
                                    key={project.id}
                                    className={`carousel-indicator${index === activeIndex ? ' active' : ''}`}
                                    type="button"
                                    onClick={() => showProject(index)}
                                    aria-label={`Show featured project ${index + 1}: ${project.title}`}
                                    aria-pressed={index === activeIndex}
                                />
                            ))}
                        </div>
                    </div>
                ) : null}
            </section>

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
        </main>
    );
}

interface FeaturedCarouselSlideProps {
    project: ProjectSummary;
    state: 'active' | 'prev1' | 'prev2' | 'next1' | 'next2' | 'hidden';
    onNavigate: NavigateFn;
}

function FeaturedCarouselSlide({
    project,
    state,
    onNavigate
}: FeaturedCarouselSlideProps) {
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
