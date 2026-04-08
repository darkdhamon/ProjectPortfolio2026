import { startTransition, useEffect, useState } from 'react';
import { fetchWorkHistory } from '../api/workHistory';
import type { Employer } from '../app/types';

interface UseWorkHistoryResult {
    employers: Employer[];
    isLoading: boolean;
    error: string | null;
}

export function useWorkHistory(): UseWorkHistoryResult {
    const [employers, setEmployers] = useState<Employer[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);

        void loadWorkHistory();

        return () => controller.abort();

        async function loadWorkHistory() {
            try {
                const response = await fetchWorkHistory(controller.signal, requestId);
                if (response.requestId !== requestId) {
                    return;
                }

                startTransition(() => setEmployers(response.items));
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load work history right now.');
                setEmployers([]);
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    return {
        employers,
        isLoading,
        error
    };
}
