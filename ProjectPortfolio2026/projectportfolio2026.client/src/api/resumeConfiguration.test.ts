import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { saveResumeConfigurationAsync } from './resumeConfiguration';
import { csrfCookieName, csrfHeaderName } from './http';

const fetchMock = vi.fn<typeof fetch>();

describe('resumeConfiguration api helpers', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
        document.cookie = `${csrfCookieName}=csrf-token-value`;
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
        document.cookie = `${csrfCookieName}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
    });

    it('clears disabled fields when saving a none source configuration', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({
            id: 1,
            sourceType: 'none',
            sourceUrl: null,
            displayLabel: null,
            summary: null,
            isConfigured: false
        }));

        await saveResumeConfigurationAsync({
            sourceType: 'none',
            sourceUrl: ' https://cdn.example.dev/resume.pdf ',
            displayLabel: ' Download Resume ',
            summary: ' Hidden resume link '
        });

        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(fetchMock).toHaveBeenCalledWith('/api/admin/resume-configuration', expect.objectContaining({
            credentials: 'include',
            method: 'PUT',
            headers: expect.any(Headers)
        }));

        const [, options] = fetchMock.mock.calls[0];
        const headers = options?.headers as Headers;

        expect(headers.get('Content-Type')).toBe('application/json');
        expect(headers.get(csrfHeaderName)).toBe('csrf-token-value');
        expect(options?.body).toBe(JSON.stringify({
            sourceType: 'none',
            sourceUrl: null,
            displayLabel: null,
            summary: null
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
