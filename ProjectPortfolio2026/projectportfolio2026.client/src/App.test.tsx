import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import {
    buildDetailPath,
    buildListSearch,
    buildProjectsPath,
    createRouteKey,
    formatFullDate,
    formatProjectDates,
    getProjectYearSpacerLabel,
    mergeProjects,
    parseListFilters,
    parseRoute,
    parseSkills,
    renderMarkdownParagraphs
} from './appSupport';

const fetchMock = vi.fn<typeof fetch>();
let isMobileViewport = false;

describe('App', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
        vi.stubGlobal('crypto', {
            randomUUID: vi.fn(() => 'request-1')
        });
        vi.stubGlobal('matchMedia', vi.fn((query: string) => ({
            matches: query === '(max-width: 720px)' ? isMobileViewport : false,
            media: query,
            onchange: null,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            addListener: vi.fn(),
            removeListener: vi.fn(),
            dispatchEvent: vi.fn()
        })));
    });

    afterEach(() => {
        cleanup();
        window.history.replaceState({}, '', '/');
        isMobileViewport = false;
        vi.useRealTimers();
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    it('renders the homepage carousel from the featured projects endpoint', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            items: [
                {
                    id: 42,
                    title: 'Portfolio Refresh',
                    startDate: '2025-01-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Rebuilt the public portfolio experience.',
                    isFeatured: true,
                    skills: ['React'],
                    technologies: ['TypeScript']
                },
                {
                    id: 64,
                    title: 'Launch Control',
                    startDate: '2024-06-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Release orchestration workspace.',
                    isFeatured: true,
                    skills: ['Accessibility'],
                    technologies: ['ASP.NET Core']
                }
            ]
        }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Home' })).toHaveAttribute('aria-current', 'page');
        expect(screen.getByRole('link', { name: 'Projects' })).toHaveAttribute('href', '/projects');
        expect(screen.getByRole('button', { name: 'Show next featured project' })).toBeInTheDocument();
        expect(fetchMock).toHaveBeenCalledWith('/api/projects/featured?limit=5&requestId=request-1', expect.any(Object));
    });

    it('supports desktop keyboard navigation for the homepage carousel', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            items: [
                {
                    id: 42,
                    title: 'Portfolio Refresh',
                    startDate: '2025-01-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Rebuilt the public portfolio experience.',
                    isFeatured: true,
                    skills: ['React'],
                    technologies: ['TypeScript']
                },
                {
                    id: 64,
                    title: 'Launch Control',
                    startDate: '2024-06-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Release orchestration workspace.',
                    isFeatured: true,
                    skills: ['Accessibility'],
                    technologies: ['ASP.NET Core']
                }
            ]
        }));

        render(<App />);

        const carousel = await screen.findByRole('region', { name: 'Featured project carousel' });
        fireEvent.keyDown(carousel, { key: 'ArrowRight' });

        expect(await screen.findByRole('heading', { name: 'Launch Control' })).toBeInTheDocument();
    });

    it('uses swipe-only mobile carousel behavior without visible arrows', async () => {
        isMobileViewport = true;
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            items: [
                {
                    id: 42,
                    title: 'Portfolio Refresh',
                    startDate: '2025-01-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Rebuilt the public portfolio experience.',
                    isFeatured: true,
                    skills: ['React'],
                    technologies: ['TypeScript']
                },
                {
                    id: 64,
                    title: 'Launch Control',
                    startDate: '2024-06-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Release orchestration workspace.',
                    isFeatured: true,
                    skills: ['Accessibility'],
                    technologies: ['ASP.NET Core']
                }
            ]
        }));

        render(<App />);

        const carousel = await screen.findByRole('region', { name: 'Featured project carousel' });
        expect(screen.queryByRole('button', { name: 'Show next featured project' })).not.toBeInTheDocument();

        fireEvent.touchStart(carousel, {
            changedTouches: [{ clientX: 220 }]
        });
        fireEvent.touchEnd(carousel, {
            changedTouches: [{ clientX: 80 }]
        });

        expect(await screen.findByRole('heading', { name: 'Launch Control' })).toBeInTheDocument();
    });

    it('renders published projects and summary counts for the list view', async () => {
        window.history.replaceState({}, '', '/projects');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            items: [
                {
                    id: 42,
                    title: 'Portfolio Refresh',
                    startDate: '2025-01-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Rebuilt the public portfolio experience.',
                    isFeatured: true,
                    skills: ['React'],
                    technologies: ['TypeScript']
                },
                {
                    id: 43,
                    title: 'Archive Cleanup',
                    startDate: '2024-01-01',
                    endDate: '2024-08-15',
                    primaryImageUrl: null,
                    shortDescription: 'Closed out a legacy migration.',
                    isFeatured: false,
                    skills: ['Testing'],
                    technologies: ['SQL Server']
                }
            ],
            page: 1,
            pageSize: 6,
            totalCount: 2,
            hasMore: false,
            availableSkills: ['React', 'Testing']
        }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' })).toBeInTheDocument();
        expect(screen.getByText('Published Projects')).toBeInTheDocument();
        expect(screen.getByText('Visible Cards')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Projects' })).toHaveAttribute('aria-current', 'page');
        expect(screen.getByRole('button', { name: /Timeline/i })).toBeDisabled();
        expect(screen.getAllByText('Coming Soon').length).toBeGreaterThan(0);
        expect(screen.getByRole('button', { name: 'React' })).toHaveAttribute('aria-pressed', 'false');
        expect(screen.getByLabelText('Projects ending in Present')).toBeInTheDocument();
        expect(screen.getByLabelText('Projects ending in 2024')).toBeInTheDocument();
        expect(fetchMock).toHaveBeenCalledWith('/api/projects?page=1&pageSize=6&requestId=request-1', expect.any(Object));
    });

    it('shows loading and empty states for the list view', async () => {
        window.history.replaceState({}, '', '/projects');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            items: [],
            page: 1,
            pageSize: 6,
            totalCount: 0,
            hasMore: false,
            availableSkills: []
        }));

        render(<App />);

        expect(screen.getByText('Loading skill filters...')).toBeInTheDocument();
        expect(screen.getByText('Loading published projects...')).toBeInTheDocument();
        expect(await screen.findByText('No published projects matched the current search and skill filters.')).toBeInTheDocument();
        expect(screen.getByText('Skill filters will appear once published projects are available.')).toBeInTheDocument();
    });

    it('updates search and skill filters from list interactions', async () => {
        window.history.replaceState({}, '', '/projects');
        fetchMock
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 42,
                        title: 'Portfolio Refresh',
                        startDate: '2025-01-01',
                        endDate: null,
                        primaryImageUrl: null,
                        shortDescription: 'Rebuilt the public portfolio experience.',
                        isFeatured: true,
                        skills: ['React'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React', 'Testing']
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 43,
                        title: 'React Search Result',
                        startDate: '2024-01-01',
                        endDate: null,
                        primaryImageUrl: null,
                        shortDescription: 'Filtered by search.',
                        isFeatured: false,
                        skills: ['React'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React', 'Testing']
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 43,
                        title: 'React Search Result',
                        startDate: '2024-01-01',
                        endDate: null,
                        primaryImageUrl: null,
                        shortDescription: 'Filtered by search.',
                        isFeatured: false,
                        skills: ['React'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React', 'Testing']
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 44,
                        title: 'Testing Skill Result',
                        startDate: '2023-01-01',
                        endDate: '2023-04-01',
                        primaryImageUrl: null,
                        shortDescription: 'Filtered by selected skill.',
                        isFeatured: false,
                        skills: ['Testing'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React', 'Testing']
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 44,
                        title: 'Testing Skill Result',
                        startDate: '2023-01-01',
                        endDate: '2023-04-01',
                        primaryImageUrl: null,
                        shortDescription: 'Filtered by selected skill.',
                        isFeatured: false,
                        skills: ['Testing'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React', 'Testing']
            }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' })).toBeInTheDocument();

        fireEvent.change(screen.getByRole('searchbox', { name: 'Search projects' }), {
            target: { value: 'React' }
        });

        expect(await screen.findByRole('heading', { name: 'React Search Result' })).toBeInTheDocument();
        expect(window.location.search).toBe('?search=React');

        fireEvent.click(screen.getByRole('button', { name: 'Testing' }));

        expect(await screen.findByRole('heading', { name: 'Testing Skill Result' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Testing' })).toHaveAttribute('aria-pressed', 'true');
        expect(window.location.search).toBe('?search=React&skills=Testing');
    });

    it('renders the detail view and preserves list filters in the back link', async () => {
        window.history.replaceState({}, '', '/projects/42?search=react&skills=Testing');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            id: 42,
            title: 'Portfolio Refresh',
            startDate: '2025-01-01',
            endDate: null,
            primaryImageUrl: null,
            shortDescription: 'Rebuilt the public portfolio experience.',
            longDescriptionMarkdown: '## Overview\n\nAdded filtering and detail routing.',
            gitHubUrl: 'https://github.com/example/repo',
            demoUrl: 'https://example.com/demo',
            isPublished: true,
            isFeatured: true,
            screenshots: [],
            developerRoles: ['Frontend Engineer'],
            technologies: ['React'],
            skills: ['Testing'],
            collaborators: [],
            milestones: []
        }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Projects' })).toHaveAttribute('aria-current', 'page');
        expect(screen.getByRole('link', { name: 'Back to project list' })).toHaveAttribute('href', '/projects?search=react&skills=Testing');
    });

    it('renders rich project detail sections and media fallbacks', async () => {
        window.history.replaceState({}, '', '/projects/77');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            id: 77,
            title: 'Launch Control',
            startDate: '2024-01-01',
            endDate: '2024-05-01',
            primaryImageUrl: 'https://cdn.example.com/primary.png',
            shortDescription: 'Mission dashboard for release management.',
            longDescriptionMarkdown: '## Outcome\n\nShipped coordinated releases.',
            gitHubUrl: 'https://github.com/example/launch-control',
            demoUrl: 'https://example.com/launch-control',
            isPublished: true,
            isFeatured: true,
            screenshots: [
                {
                    imageUrl: 'https://cdn.example.com/shot-1.png',
                    caption: 'Overview dashboard',
                    sortOrder: 1
                },
                {
                    imageUrl: 'https://cdn.example.com/shot-2.png',
                    caption: null,
                    sortOrder: 2
                }
            ],
            developerRoles: ['Lead Engineer'],
            technologies: ['React', 'ASP.NET Core'],
            skills: ['Testing', 'Accessibility'],
            collaborators: [
                {
                    name: 'Taylor Dev',
                    gitHubProfileUrl: 'https://github.com/taylor',
                    websiteUrl: 'https://taylor.example.com',
                    photoUrl: 'https://cdn.example.com/taylor.png',
                    roles: ['Designer', 'QA']
                }
            ],
            milestones: [
                {
                    title: 'Public beta',
                    targetDate: '2024-03-15',
                    completedOn: '2024-03-10',
                    description: 'Enabled early access testing.'
                },
                {
                    title: 'General availability',
                    targetDate: '2024-05-01',
                    completedOn: null,
                    description: null
                }
            ]
        }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Launch Control' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Overview' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Screenshots' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Collaborators' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Milestones' })).toBeInTheDocument();
        expect(screen.getByRole('region', { name: 'Project screenshot carousel' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Show next screenshot' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Live Demo' })).toHaveAttribute('href', 'https://example.com/launch-control');
        expect(screen.getByRole('link', { name: 'Source' })).toHaveAttribute('href', 'https://github.com/example/launch-control');
        expect(screen.getByText('Completed')).toBeInTheDocument();
        expect(screen.getByText('Planned')).toBeInTheDocument();
        expect(screen.getByText('Taylor Dev')).toBeInTheDocument();
        expect(screen.getByText('Designer | QA')).toBeInTheDocument();
        expect(screen.getByText('Overview dashboard')).toBeInTheDocument();
        expect(screen.getByText('Screenshot 1 of 2')).toBeInTheDocument();
        expect(screen.getAllByAltText('Launch Control screenshot 2')).toHaveLength(2);

        fireEvent.click(screen.getAllByRole('button', { name: 'Open screenshot 1 in fullscreen' })[0]);
        expect(screen.getByRole('dialog', { name: 'Fullscreen screenshot viewer' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Close image' })).toBeInTheDocument();
        fireEvent.click(screen.getByRole('button', { name: 'Close image' }));
        expect(screen.queryByRole('dialog', { name: 'Fullscreen screenshot viewer' })).not.toBeInTheDocument();

        fireEvent.click(screen.getAllByRole('button', { name: 'Open screenshot 1 in fullscreen' })[0]);
        expect(screen.getByRole('dialog', { name: 'Fullscreen screenshot viewer' })).toBeInTheDocument();
        fireEvent.keyDown(window, { key: 'Escape' });
        expect(screen.queryByRole('dialog', { name: 'Fullscreen screenshot viewer' })).not.toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Show next screenshot' }));
        expect(screen.getByText('Screenshot 2 of 2')).toBeInTheDocument();
        expect(screen.getByText('Launch Control interface preview.')).toBeInTheDocument();
        expect(screen.getAllByAltText('Overview dashboard')).toHaveLength(2);

        const collaboratorImage = screen.getByAltText('Taylor Dev profile');
        fireEvent.error(collaboratorImage);
        expect(screen.getByAltText('Taylor Dev profile')).toHaveAttribute('src', expect.stringContaining('Profile-Placeholder'));
    });

    it('shows only a single slide when the detail view has one screenshot', async () => {
        window.history.replaceState({}, '', '/projects/88');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            id: 88,
            title: 'Single Shot',
            startDate: '2024-01-01',
            endDate: null,
            primaryImageUrl: 'https://cdn.example.com/primary.png',
            shortDescription: 'Single screenshot project.',
            longDescriptionMarkdown: 'Only one screenshot is available.',
            gitHubUrl: null,
            demoUrl: null,
            isPublished: true,
            isFeatured: false,
            screenshots: [
                {
                    imageUrl: 'https://cdn.example.com/single-shot.png',
                    caption: 'Single view',
                    sortOrder: 1
                }
            ],
            developerRoles: [],
            technologies: [],
            skills: [],
            collaborators: [],
            milestones: []
        }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Single Shot' })).toBeInTheDocument();
        expect(screen.getByText('Screenshot 1 of 1')).toBeInTheDocument();
        expect(screen.getAllByAltText('Single view')).toHaveLength(1);
        expect(screen.queryByRole('button', { name: 'Show next screenshot' })).not.toBeInTheDocument();
        expect(screen.queryByLabelText('Project screenshot selection')).not.toBeInTheDocument();
    });

    it('lets the detail back link navigate to the list route without scrolling reset', async () => {
        window.history.replaceState({}, '', '/projects/42?search=react');
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-1',
            id: 42,
            title: 'Portfolio Refresh',
            startDate: '2025-01-01',
            endDate: null,
            primaryImageUrl: null,
            shortDescription: 'Rebuilt the public portfolio experience.',
            longDescriptionMarkdown: '## Overview\n\nAdded filtering and detail routing.',
            gitHubUrl: null,
            demoUrl: null,
            isPublished: true,
            isFeatured: false,
            screenshots: [],
            developerRoles: [],
            technologies: [],
            skills: [],
            collaborators: [],
            milestones: []
        }));

        render(<App />);

        fireEvent.click(await screen.findByRole('link', { name: 'Back to project list' }));

        expect(window.location.pathname).toBe('/projects');
        expect(window.location.search).toBe('?search=react');
    });

    it('shows a not found message when the project detail endpoint returns 404', async () => {
        window.history.replaceState({}, '', '/projects/999');
        fetchMock.mockResolvedValueOnce(new Response(null, { status: 404 }));

        render(<App />);

        expect(await screen.findByText('That project could not be found or is no longer public.')).toBeInTheDocument();
    });

    it('surfaces API failures for the homepage', async () => {
        fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
            message: 'Featured projects are temporarily unavailable.'
        }), {
            status: 400,
            headers: {
                'Content-Type': 'application/json'
            }
        }));

        render(<App />);

        expect(await screen.findByText('Featured projects are temporarily unavailable.')).toBeInTheDocument();
    });

    it('retries the homepage request when the API is still starting up', async () => {
        fetchMock
            .mockResolvedValueOnce(new Response('<!doctype html><html><body>Starting up</body></html>', {
                status: 503,
                headers: {
                    'Content-Type': 'text/html'
                }
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 42,
                        title: 'Portfolio Refresh',
                        startDate: '2025-01-01',
                        endDate: null,
                        primaryImageUrl: null,
                        shortDescription: 'Rebuilt the public portfolio experience.',
                        isFeatured: true,
                        skills: ['React'],
                        technologies: ['TypeScript']
                    }
                ]
            }));

        render(<App />);

        expect(fetchMock).toHaveBeenCalledTimes(1);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' }, { timeout: 5000 })).toBeInTheDocument();
        expect(fetchMock).toHaveBeenCalledTimes(2);
    }, 7000);

    it('retries the project list request when startup returns invalid JSON', async () => {
        window.history.replaceState({}, '', '/projects');
        fetchMock
            .mockResolvedValueOnce(new Response('<!doctype html><html><body>Starting up</body></html>', {
                status: 503,
                headers: {
                    'Content-Type': 'text/html'
                }
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                items: [
                    {
                        id: 42,
                        title: 'Portfolio Refresh',
                        startDate: '2025-01-01',
                        endDate: null,
                        primaryImageUrl: null,
                        shortDescription: 'Rebuilt the public portfolio experience.',
                        isFeatured: true,
                        skills: ['React'],
                        technologies: ['TypeScript']
                    }
                ],
                page: 1,
                pageSize: 6,
                totalCount: 1,
                hasMore: false,
                availableSkills: ['React']
            }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' }, { timeout: 5000 })).toBeInTheDocument();
        expect(fetchMock).toHaveBeenCalledTimes(2);
    }, 7000);

    it('retries the project detail request when startup returns invalid JSON', async () => {
        window.history.replaceState({}, '', '/projects/42');
        fetchMock
            .mockResolvedValueOnce(new Response('<!doctype html><html><body>Starting up</body></html>', {
                status: 503,
                headers: {
                    'Content-Type': 'text/html'
                }
            }))
            .mockResolvedValueOnce(jsonResponse({
                requestId: 'request-1',
                id: 42,
                title: 'Portfolio Refresh',
                startDate: '2025-01-01',
                endDate: null,
                primaryImageUrl: null,
                shortDescription: 'Rebuilt the public portfolio experience.',
                longDescriptionMarkdown: 'Overview copy',
                gitHubUrl: null,
                demoUrl: null,
                isPublished: true,
                isFeatured: true,
                screenshots: [],
                developerRoles: ['Frontend Engineer'],
                technologies: ['TypeScript'],
                skills: ['React'],
                collaborators: [],
                milestones: []
            }));

        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Portfolio Refresh' }, { timeout: 5000 })).toBeInTheDocument();
        expect(fetchMock).toHaveBeenCalledTimes(2);
    }, 7000);

    it('surfaces detail API failures', async () => {
        window.history.replaceState({}, '', '/projects/404');
        fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
            message: 'The project detail endpoint is unavailable.'
        }), {
            status: 400,
            headers: {
                'Content-Type': 'application/json'
            }
        }));

        render(<App />);

        expect(await screen.findByText('The project detail endpoint is unavailable.')).toBeInTheDocument();
    });

    it('routes signed-out admin navigation through the login page', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            isAuthenticated: false,
            isAdmin: false
        }));

        render(<App />);

        fireEvent.click(await screen.findByRole('button', { name: /Admin/ }));

        expect(await screen.findByRole('heading', { name: 'Sign in through the admin entry point.' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument();
        expect(screen.queryByText('Welcome')).not.toBeInTheDocument();
        expect(window.location.pathname).toBe('/login');
        expect(window.location.search).toBe('?redirect=%2Fadmin');
    });

    it('signs in through the mock login page and reveals admin account navigation', async () => {
        window.history.replaceState({}, '', '/login?redirect=%2Fadmin');
        fetchMock
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: false,
                isAdmin: false
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'admin',
                email: 'admin@example.com',
                displayName: 'admin'
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'admin',
                email: 'admin@example.com',
                displayName: 'admin'
            }));
        render(<App />);

        fireEvent.change(screen.getByLabelText('Username or email'), {
            target: { value: 'admin@example.com' }
        });
        fireEvent.change(screen.getByLabelText('Password'), {
            target: { value: 'Password123!' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Log In' }));

        expect(await screen.findByRole('heading', { name: 'Admin dashboard mockup' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Account Settings' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Log out' })).toBeInTheDocument();
        expect(screen.getByText('Welcome')).toBeInTheDocument();
        expect(screen.getAllByText('admin').length).toBeGreaterThan(0);
        expect(screen.getByText('Signed in as admin')).toBeInTheDocument();
        expect(window.location.pathname).toBe('/admin');
    });

    it('redirects direct account access to login when signed out', async () => {
        window.history.replaceState({}, '', '/admin/account');
        fetchMock
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: false,
                isAdmin: false
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: false,
                isAdmin: false
            }));
        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Sign in through the admin entry point.' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument();
        expect(window.location.pathname).toBe('/login');
        expect(window.location.search).toBe('?redirect=%2Fadmin%2Faccount');
    });

    it('lets the signed-in admin edit account settings and log out', async () => {
        window.history.replaceState({}, '', '/login?redirect=%2Fadmin');
        fetchMock
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: false,
                isAdmin: false
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'admin',
                email: 'admin@example.com',
                displayName: 'admin'
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'admin',
                email: 'admin@example.com',
                displayName: 'admin'
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'admin',
                email: 'admin@example.com',
                displayName: 'admin'
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: true,
                isAdmin: true,
                userName: 'editor-admin',
                email: 'admin@example.com',
                displayName: 'Portfolio Owner'
            }))
            .mockResolvedValueOnce(jsonResponse({
                signedOut: true
            }))
            .mockResolvedValueOnce(jsonResponse({
                isAuthenticated: false,
                isAdmin: false
            }));
        render(<App />);

        fireEvent.click(screen.getByRole('button', { name: 'Log In' }));
        fireEvent.click(await screen.findByRole('link', { name: 'Account Settings' }));

        expect(await screen.findByRole('heading', { name: 'Manage your current admin account' })).toBeInTheDocument();

        fireEvent.change(screen.getByLabelText('Username'), {
            target: { value: 'editor-admin' }
        });
        fireEvent.change(screen.getByLabelText('Display name'), {
            target: { value: 'Portfolio Owner' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));

        expect(await screen.findByText('Profile saved.')).toBeInTheDocument();
        expect(screen.getByText('Welcome')).toBeInTheDocument();
        expect(screen.getAllByText('Portfolio Owner').length).toBeGreaterThan(0);
        expect(screen.getByText('Signed in as Portfolio Owner')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Log out' }));

        expect(await screen.findByRole('heading', { name: 'Sign in through the admin entry point.' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument();
    });
});

describe('App helpers', () => {
    it('parses list filters from query text', () => {
        expect(parseListFilters('?search= react &skills=React, Testing,React')).toEqual({
            searchInput: 'react',
            selectedSkills: ['React', 'Testing']
        });
    });

    it('parses home, list, detail, and admin routes', () => {
        expect(parseRoute({ pathname: '/', search: '?search=react' })).toEqual({
            kind: 'home'
        });

        expect(parseRoute({ pathname: '/login', search: '?redirect=%2Fadmin' })).toEqual({
            kind: 'login',
            redirectTo: '/admin'
        });

        expect(parseRoute({ pathname: '/admin', search: '' })).toEqual({
            kind: 'admin'
        });

        expect(parseRoute({ pathname: '/admin/account', search: '' })).toEqual({
            kind: 'admin-account'
        });

        expect(parseRoute({ pathname: '/projects', search: '?search=react' })).toEqual({
            kind: 'list',
            filters: {
                searchInput: 'react',
                selectedSkills: []
            }
        });

        expect(parseRoute({ pathname: '/projects/42', search: '?skills=React' })).toEqual({
            kind: 'detail',
            projectId: 42,
            listSearch: '?skills=React'
        });
    });

    it('builds route state helpers deterministically', () => {
        expect(buildListSearch({
            searchInput: '  React  ',
            selectedSkills: ['Testing', 'React']
        })).toBe('?search=React&skills=Testing%2CReact');

        expect(buildProjectsPath('?search=React')).toBe('/projects?search=React');
        expect(buildDetailPath(42, '?search=React')).toBe('/projects/42?search=React');
        expect(createRouteKey({
            searchInput: ' React ',
            selectedSkills: ['Testing', 'React']
        })).toBe(JSON.stringify({
            searchInput: 'React',
            selectedSkills: ['React', 'Testing']
        }));
    });

    it('deduplicates parsed skills and merged projects', () => {
        expect(parseSkills('React, Testing, React, ,')).toEqual(['React', 'Testing']);
        expect(mergeProjects(
            [{ id: 1 }, { id: 2 }],
            [{ id: 2 }, { id: 3 }]
        )).toEqual([{ id: 1 }, { id: 2 }, { id: 3 }]);
    });

    it('formats dates and markdown paragraphs for display', () => {
        expect(formatProjectDates('2025-01-01', null)).toContain('Present');
        expect(formatFullDate('2025-04-02')).toContain('2025');
        expect(getProjectYearSpacerLabel(null)).toBe('Present');
        expect(getProjectYearSpacerLabel('2024-08-15')).toBe('2024');

        const paragraphs = renderMarkdownParagraphs('## Heading\n\nBody copy');
        expect(paragraphs).toHaveLength(2);
        expect(paragraphs[0].props.children).toBe('Heading');
    });
});

function jsonResponse(payload: object) {
    return new Response(JSON.stringify(payload), {
        status: 200,
        headers: {
            'Content-Type': 'application/json'
        }
    });
}
