import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchAdminSocialLinks, saveAdminSocialLinks } from './socialLinks';
import { csrfCookieName, csrfHeaderName } from './http';

const fetchMock = vi.fn<typeof fetch>();

describe('socialLinks api helpers', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
        document.cookie = `${csrfCookieName}=csrf-token-value`;
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
        document.cookie = `${csrfCookieName}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
    });

    it('loads the admin social links list from the admin endpoint', async () => {
        const sample = [
            {
                id: 1,
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/darkdhamon',
                handle: '@darkdhamon',
                summary: 'Main portfolio source',
                sortOrder: 1,
                isVisible: true
            }
        ];
        fetchMock.mockResolvedValueOnce(jsonResponse(sample));

        const links = await fetchAdminSocialLinks();

        expect(links).toEqual(sample);
        expect(fetchMock).toHaveBeenCalledWith('/api/admin/social-links', expect.objectContaining({
            method: 'GET'
        }));
    });

    it('saves admin social links with deterministic sort order and CSRF headers', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse([
            {
                id: 1,
                platform: 'linkedin',
                label: 'LinkedIn',
                url: 'https://www.linkedin.com/in/example',
                sortOrder: 1,
                isVisible: true
            }
        ]));

        await saveAdminSocialLinks([
            {
                platform: 'linkedin',
                label: 'LinkedIn',
                url: 'https://www.linkedin.com/in/example',
                sortOrder: 9,
                isVisible: true
            },
            {
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/example',
                sortOrder: 4,
                isVisible: false
            }
        ]);

        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(fetchMock).toHaveBeenCalledWith('/api/admin/social-links', expect.objectContaining({
            credentials: 'include',
            method: 'PUT',
            headers: expect.any(Headers)
        }));

        const [, options] = fetchMock.mock.calls[0];
        const headers = options?.headers as Headers;
        expect(headers.get(csrfHeaderName)).toBe('csrf-token-value');
        const body = JSON.parse((options?.body as string | undefined) ?? '{}');
        expect(body.socialLinks).toEqual([
            {
                platform: 'linkedin',
                label: 'LinkedIn',
                url: 'https://www.linkedin.com/in/example',
                sortOrder: 1,
                isVisible: true
            },
            {
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/example',
                sortOrder: 2,
                isVisible: false
            }
        ]);
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
