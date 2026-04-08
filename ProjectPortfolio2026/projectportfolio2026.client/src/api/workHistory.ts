import { fetchJsonWithStartupRetry } from '../app/api';
import type { WorkHistoryResponse } from '../app/types';

export async function fetchWorkHistory(signal: AbortSignal, requestId: string) {
    return await fetchJsonWithStartupRetry<WorkHistoryResponse>(
        `/api/work-history?requestId=${requestId}`,
        {
            signal
        },
        'Unable to load work history right now.'
    );
}
