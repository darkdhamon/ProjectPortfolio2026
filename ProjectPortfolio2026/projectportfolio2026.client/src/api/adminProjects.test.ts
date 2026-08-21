import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
    fetchAdminProjects,
    setFeaturedProjectOrder,
    setProjectArchivedState,
    setProjectFeaturedState
} from './adminProjects';
import { csrfCookieName, csrfHeaderName } from './http';

const fetchMock = vi.fn<typeof fetch>();

describe('adminProjects api helpers', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
        vi.stubGlobal('crypto', {
            randomUUID: vi.fn(() => 'request-103')
        });
        document.cookie = `${csrfCookieName}=csrf-token-value`;
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
        document.cookie = `${csrfCookieName}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
    });

    it('fetches project summaries for admin with a default page', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            requestId: 'request-103',
            items: [
                {
                    id: 10,
                    title: 'Portfolio Refresh',
                    startDate: '2026-01-01',
                    endDate: null,
                    primaryImageUrl: null,
                    shortDescription: 'Featured admin scope.',
                    isFeatured: true,
                    featuredOrder: 0,
                    skills: ['React'],
                    technologies: ['TypeScript']
                }
            ],
            page: 1,
            pageSize: 50,
            totalCount: 1,
            hasMore: false,
            availableSkills: ['React', 'TypeScript']
        }));

        const projects = await fetchAdminProjects();

        expect(projects).toHaveLength(1);
        expect(fetchMock).toHaveBeenCalledWith('/api/admin/projects?page=1&pageSize=50&requestId=request-103', expect.objectContaining({
            signal: expect.any(AbortSignal)
        }));
    });

    it('loads every admin project page', async () => {
        fetchMock
            .mockResolvedValueOnce(jsonResponse({
                items: [{ id: 10, title: 'First page' }],
                page: 1,
                pageSize: 50,
                totalCount: 2,
                hasMore: true,
                availableSkills: []
            }))
            .mockResolvedValueOnce(jsonResponse({
                items: [{ id: 60, title: 'Second page' }],
                page: 2,
                pageSize: 50,
                totalCount: 2,
                hasMore: false,
                availableSkills: []
            }));

        const projects = await fetchAdminProjects();

        expect(projects.map(project => project.id)).toEqual([10, 60]);
        expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/admin/projects?page=2&pageSize=50&requestId=request-103', expect.anything());
    });

    it('sends featured-state updates through the admin endpoint', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            id: 10,
            title: 'Featured Candidate',
            startDate: '2026-01-01',
            endDate: null,
            primaryImageUrl: null,
            shortDescription: 'Candidate project',
            isFeatured: true,
            featuredOrder: 0,
            skills: [],
            technologies: []
        }));

        await setProjectFeaturedState(10, true);

        expect(fetchMock).toHaveBeenCalledTimes(1);
        const [, options] = fetchMock.mock.calls[0];
        const headers = options?.headers as Headers;

        expect(headers.get('Content-Type')).toBe('application/json');
        expect(headers.get(csrfHeaderName)).toBe('csrf-token-value');
        expect(options?.body).toBe('{"isFeatured":true}');
    });

    it('sends featured-order updates through the admin endpoint', async () => {
        fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));

        await setFeaturedProjectOrder([5, 6, 7]);

        expect(fetchMock).toHaveBeenCalledWith('/api/admin/projects/featured-order', expect.objectContaining({
            method: 'PUT',
            body: '{"projectIds":[5,6,7]}'
        }));
        const [, options] = fetchMock.mock.calls[0];
        const headers = options?.headers as Headers;

        expect(headers.get('Content-Type')).toBe('application/json');
        expect(headers.get(csrfHeaderName)).toBe('csrf-token-value');
    });

    it.each([
        [true, 'archive'],
        [false, 'restore']
    ])('sends archived-state updates through the admin endpoint', async (isArchived, route) => {
        fetchMock.mockResolvedValueOnce(jsonResponse({ id: 10 }));

        await setProjectArchivedState(10, isArchived);

        expect(fetchMock).toHaveBeenCalledWith(`/api/admin/projects/10/${route}`, expect.objectContaining({
            method: 'PUT'
        }));
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
