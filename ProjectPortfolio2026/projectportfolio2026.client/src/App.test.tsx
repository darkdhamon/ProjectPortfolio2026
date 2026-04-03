import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import {
    buildDetailPath,
    buildListSearch,
    createRouteKey,
    formatFullDate,
    formatProjectDates,
    mergeProjects,
    parseListFilters,
    parseRoute,
    parseSkills,
    renderMarkdownParagraphs
} from './appSupport';

const fetchMock = vi.fn<typeof fetch>();

describe('App', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
        vi.stubGlobal('crypto', {
            randomUUID: vi.fn(() => 'request-1')
        });
    });

    afterEach(() => {
        window.history.replaceState({}, '', '/');
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    it('renders published projects and summary counts for the list view', async () => {
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
        expect(screen.getByText('Published Projects')).toBeInTheDocument();
        expect(screen.getByText('Visible Cards')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'React' })).toHaveAttribute('aria-pressed', 'false');
        expect(fetchMock).toHaveBeenCalledWith('/api/projects?page=1&pageSize=6&requestId=request-1', expect.any(Object));
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
        expect(screen.getByRole('link', { name: 'Back to project list' })).toHaveAttribute('href', '/?search=react&skills=Testing');
    });

    it('shows a not found message when the project detail endpoint returns 404', async () => {
        window.history.replaceState({}, '', '/projects/999');
        fetchMock.mockResolvedValueOnce(new Response(null, { status: 404 }));

        render(<App />);

        expect(await screen.findByText('That project could not be found or is no longer public.')).toBeInTheDocument();
    });

    it('surfaces API failures for the list view', async () => {
        fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
            message: 'Projects are temporarily unavailable.'
        }), {
            status: 503,
            headers: {
                'Content-Type': 'application/json'
            }
        }));

        render(<App />);

        expect(await screen.findByText('Projects are temporarily unavailable.')).toBeInTheDocument();
    });
});

describe('App helpers', () => {
    it('parses list filters from query text', () => {
        expect(parseListFilters('?search= react &skills=React, Testing,React')).toEqual({
            searchInput: 'react',
            selectedSkills: ['React', 'Testing']
        });
    });

    it('parses list and detail routes', () => {
        expect(parseRoute({ pathname: '/', search: '?search=react' })).toEqual({
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
