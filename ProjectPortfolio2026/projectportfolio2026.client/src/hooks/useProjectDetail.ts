import { startTransition, useEffect, useState } from 'react';
import {
    fetchResponsePayloadWithStartupRetry,
    RetryableApiError,
    startupRetryMessage
} from '../app/api';
import type { ProjectDetail } from '../app/types';

interface UseProjectDetailResult {
    project: ProjectDetail | null;
    isLoading: boolean;
    error: string | null;
    isMissing: boolean;
}

export function useProjectDetail(projectId: number): UseProjectDetailResult {
    const [project, setProject] = useState<ProjectDetail | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isMissing, setIsMissing] = useState(false);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);
        setIsMissing(false);

        void loadProject();

        return () => controller.abort();

        async function loadProject() {
            try {
                const query = new URLSearchParams({ requestId });
                const { response, payload } = await fetchResponsePayloadWithStartupRetry<ProjectDetail>(
                    `/api/projects/${projectId}?${query.toString()}`,
                    {
                        signal: controller.signal
                    },
                    'Unable to load this project right now.',
                    [404]
                );

                if (response.status === 404) {
                    setIsMissing(true);
                    setProject(null);
                    return;
                }

                if (payload === null) {
                    throw new RetryableApiError(startupRetryMessage);
                }

                const projectResponse = payload as ProjectDetail;

                if (projectResponse.requestId !== requestId) {
                    return;
                }

                startTransition(() => setProject(projectResponse));
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load this project right now.');
                setProject(null);
            } finally {
                setIsLoading(false);
            }
        }
    }, [projectId]);

    return {
        project,
        isLoading,
        error,
        isMissing
    };
}
