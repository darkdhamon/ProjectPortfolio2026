import {
    fetchAuthJson,
    fetchResponsePayloadWithStartupRetry,
    RetryableApiError,
    startupRetryMessage
} from './http';
import type { ResumeConfiguration, ResumeSourceType } from '../app/types';

export interface ResumeConfigurationDraft {
    sourceType: ResumeSourceType;
    sourceUrl: string;
    displayLabel: string;
    summary: string;
}

export async function fetchPublicResumeConfiguration(signal: AbortSignal) {
    const requestId = crypto.randomUUID();
    const query = new URLSearchParams({ requestId });
    const { response, payload } = await fetchResponsePayloadWithStartupRetry<ResumeConfiguration>(
        `/api/resume-configuration?${query.toString()}`,
        {
            signal
        },
        'Unable to load the public resume source right now.',
        [404]
    );

    if (response.status === 404) {
        return {
            isMissing: true,
            configuration: null
        };
    }

    if (payload === null) {
        throw new RetryableApiError(startupRetryMessage);
    }

    return {
        isMissing: false,
        configuration: payload as ResumeConfiguration
    };
}

export async function fetchAdminResumeConfiguration(signal?: AbortSignal) {
    const { payload } = await fetchAuthJson<ResumeConfiguration>(
        '/api/admin/resume-configuration',
        {
            method: 'GET',
            signal
        },
        'Unable to load resume configuration.'
    );

    return payload as ResumeConfiguration | null;
}

export async function saveResumeConfigurationAsync(draft: ResumeConfigurationDraft) {
    const { payload } = await fetchAuthJson<ResumeConfiguration>(
        '/api/admin/resume-configuration',
        {
            method: 'PUT',
            body: JSON.stringify({
                sourceType: draft.sourceType,
                sourceUrl: draft.sourceUrl.trim() || null,
                displayLabel: draft.displayLabel.trim() || null,
                summary: draft.summary.trim() || null
            })
        },
        'Unable to save resume configuration.'
    );

    return payload as ResumeConfiguration | null;
}
