import { useEffect, useState } from 'react';
import { fetchPublicResumeConfiguration } from '../api/resumeConfiguration';
import type { ResumeConfiguration } from '../app/types';

interface UseResumeConfigurationResult {
    configuration: ResumeConfiguration | null;
    isLoading: boolean;
    error: string | null;
    isMissing: boolean;
}

export function useResumeConfiguration(): UseResumeConfigurationResult {
    const [configuration, setConfiguration] = useState<ResumeConfiguration | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isMissing, setIsMissing] = useState(false);

    useEffect(() => {
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);
        setIsMissing(false);

        void loadConfiguration();

        return () => controller.abort();

        async function loadConfiguration() {
            try {
                const result = await fetchPublicResumeConfiguration(controller.signal);
                setIsMissing(result.isMissing);
                setConfiguration(result.configuration);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setConfiguration(null);
                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load the public resume source right now.');
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    return {
        configuration,
        isLoading,
        error,
        isMissing
    };
}
