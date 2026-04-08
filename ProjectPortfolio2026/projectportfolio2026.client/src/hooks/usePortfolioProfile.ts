import { useEffect, useState } from 'react';
import {
    fetchResponsePayloadWithStartupRetry,
    RetryableApiError,
    startupRetryMessage
} from '../app/api';
import type { PortfolioProfile } from '../app/types';

interface UsePortfolioProfileResult {
    profile: PortfolioProfile | null;
    isLoading: boolean;
    error: string | null;
    isMissing: boolean;
}

export function usePortfolioProfile(): UsePortfolioProfileResult {
    const [profile, setProfile] = useState<PortfolioProfile | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isMissing, setIsMissing] = useState(false);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);
        setIsMissing(false);

        void loadProfile();

        return () => controller.abort();

        async function loadProfile() {
            try {
                const query = new URLSearchParams({ requestId });
                const { response, payload } = await fetchResponsePayloadWithStartupRetry<PortfolioProfile>(
                    `/api/portfolio-profile?${query.toString()}`,
                    {
                        signal: controller.signal
                    },
                    'Unable to load the contact profile right now.',
                    [404]
                );

                if (response.status === 404) {
                    setIsMissing(true);
                    setProfile(null);
                    return;
                }

                if (payload === null) {
                    throw new RetryableApiError(startupRetryMessage);
                }

                const profileResponse = payload as PortfolioProfile;
                if (profileResponse.requestId !== requestId) {
                    return;
                }

                setProfile(profileResponse);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load the contact profile right now.');
                setProfile(null);
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    return {
        profile,
        isLoading,
        error,
        isMissing
    };
}
