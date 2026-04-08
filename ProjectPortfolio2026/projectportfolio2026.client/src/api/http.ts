export interface ApiErrorResponse {
    message?: string;
}

export const startupRetryMessage = 'The portfolio API is still starting up. Please wait a moment and try again.';
const startupRetryDelayMs = 2000;
const startupRetryAttempts = 2;

export class RetryableApiError extends Error {
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

export async function readResponsePayload<TPayload>(response: Response) {
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

export async function fetchJsonWithStartupRetry<TPayload>(
    input: string,
    init: RequestInit & { signal: AbortSignal },
    fallbackMessage: string
) {
    const { payload } = await fetchResponsePayloadWithStartupRetry<TPayload>(input, init, fallbackMessage);

    if (payload === null) {
        throw new RetryableApiError(startupRetryMessage);
    }

    return payload as TPayload;
}

export async function fetchResponsePayloadWithStartupRetry<TPayload>(
    input: string,
    init: RequestInit & { signal: AbortSignal },
    fallbackMessage: string,
    acceptedErrorStatuses: number[] = []
) {
    let attempt = 0;

    while (true) {
        try {
            const response = await fetch(input, init);
            const payload = await readResponsePayload<TPayload>(response);

            if (!response.ok && !acceptedErrorStatuses.includes(response.status)) {
                const apiMessage = isApiErrorResponse(payload) ? payload.message : undefined;

                if (response.status >= 500) {
                    throw new RetryableApiError(apiMessage ?? startupRetryMessage);
                }

                throw new Error(apiMessage ?? fallbackMessage);
            }

            return { response, payload };
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

export async function fetchAuthJson<TPayload>(
    input: string,
    init: RequestInit,
    fallbackMessage: string,
    acceptedErrorStatuses: number[] = []
) {
    const response = await fetch(input, {
        credentials: 'include',
        ...init,
        headers: {
            'Content-Type': 'application/json',
            ...(init.headers ?? {})
        }
    });

    const payload = await readResponsePayload<TPayload>(response);

    if (!response.ok && !acceptedErrorStatuses.includes(response.status)) {
        const apiMessage = isApiErrorResponse(payload) ? payload.message : undefined;
        throw new Error(apiMessage ?? fallbackMessage);
    }

    return { response, payload };
}
