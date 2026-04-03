import { useEffect, useState } from 'react';
import { fetchJsonWithStartupRetry } from '../app/api';
import type { FeaturedProjectsResponse, ProjectSummary } from '../app/types';

interface UseFeaturedProjectsResult {
    projects: ProjectSummary[];
    isLoading: boolean;
    error: string | null;
}

export function useFeaturedProjects(): UseFeaturedProjectsResult {
    const [projects, setProjects] = useState<ProjectSummary[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);

        void loadFeaturedProjects();

        return () => controller.abort();

        async function loadFeaturedProjects() {
            try {
                const featuredResponse = await fetchJsonWithStartupRetry<FeaturedProjectsResponse>(
                    `/api/projects/featured?limit=5&requestId=${requestId}`,
                    {
                        signal: controller.signal
                    },
                    'Unable to load featured projects right now.'
                );

                if (featuredResponse.requestId !== requestId) {
                    return;
                }

                setProjects(featuredResponse.items);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load featured projects right now.');
                setProjects([]);
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    return {
        projects,
        isLoading,
        error
    };
}
