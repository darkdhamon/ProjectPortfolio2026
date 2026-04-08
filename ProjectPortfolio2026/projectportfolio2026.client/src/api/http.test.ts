import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
    fetchAuthJson,
    fetchJsonWithStartupRetry,
    fetchResponsePayloadWithStartupRetry,
    readResponsePayload,
    RetryableApiError,
    startupRetryMessage
} from './http';

const fetchMock = vi.fn<typeof fetch>();

describe('http api helpers', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.useRealTimers();
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    it('returns null when the response body is empty', async () => {
        const payload = await readResponsePayload(new Response('', { status: 200 }));

        expect(payload).toBeNull();
    });

    it('throws a retryable error when the response payload is invalid json', async () => {
        await expect(readResponsePayload(new Response('<html>booting</html>', { status: 503 })))
            .rejects
            .toThrow(RetryableApiError);
    });

    it('returns the payload for successful startup-aware fetches', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({ message: 'ok' }));

        const controller = new AbortController();
        const payload = await fetchJsonWithStartupRetry<{ message: string }>(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback'
        );

        expect(payload).toEqual({ message: 'ok' });
    });

    it('throws when startup-aware fetch resolves to an empty payload', async () => {
        fetchMock.mockResolvedValueOnce(new Response('', { status: 200 }));

        const controller = new AbortController();

        await expect(fetchJsonWithStartupRetry(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback'
        )).rejects.toThrow(startupRetryMessage);
    });

    it('retries retryable failures and eventually returns the accepted payload', async () => {
        vi.useFakeTimers();
        fetchMock
            .mockResolvedValueOnce(new Response(JSON.stringify({ message: 'warming up' }), {
                status: 503,
                headers: {
                    'Content-Type': 'application/json'
                }
            }))
            .mockResolvedValueOnce(jsonResponse({ value: 42 }));

        const controller = new AbortController();
        const promise = fetchResponsePayloadWithStartupRetry<{ value: number }>(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback'
        );

        await vi.advanceTimersByTimeAsync(2000);
        const result = await promise;

        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(result.payload).toEqual({ value: 42 });
    });

    it('returns accepted error statuses without throwing', async () => {
        fetchMock.mockResolvedValueOnce(new Response('', { status: 404 }));

        const controller = new AbortController();
        const result = await fetchResponsePayloadWithStartupRetry(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback',
            [404]
        );

        expect(result.response.status).toBe(404);
        expect(result.payload).toBeNull();
    });

    it('throws the api message for non-retryable failures', async () => {
        fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
            message: 'Bad request'
        }), {
            status: 400,
            headers: {
                'Content-Type': 'application/json'
            }
        }));

        const controller = new AbortController();

        await expect(fetchResponsePayloadWithStartupRetry(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback'
        )).rejects.toThrow('Bad request');
    });

    it('passes through abort errors without converting them', async () => {
        fetchMock.mockRejectedValueOnce(new DOMException('The request was aborted.', 'AbortError'));

        const controller = new AbortController();

        await expect(fetchResponsePayloadWithStartupRetry(
            '/api/test',
            {
                signal: controller.signal
            },
            'Fallback'
        )).rejects.toMatchObject({ name: 'AbortError' });
    });

    it('includes credentials and merged headers for auth requests', async () => {
        fetchMock.mockResolvedValueOnce(jsonResponse({ ok: true }));

        await fetchAuthJson(
            '/api/auth/me',
            {
                method: 'POST',
                headers: {
                    'X-Test': 'true'
                }
            },
            'Fallback'
        );

        expect(fetchMock).toHaveBeenCalledWith('/api/auth/me', expect.objectContaining({
            credentials: 'include',
            method: 'POST',
            headers: expect.objectContaining({
                'Content-Type': 'application/json',
                'X-Test': 'true'
            })
        }));
    });

    it('throws the fallback error when an auth request fails without an api message', async () => {
        fetchMock.mockResolvedValueOnce(new Response('{}', {
            status: 401,
            headers: {
                'Content-Type': 'application/json'
            }
        }));

        await expect(fetchAuthJson(
            '/api/auth/me',
            {
                method: 'GET'
            },
            'Fallback auth error'
        )).rejects.toThrow('Fallback auth error');
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
