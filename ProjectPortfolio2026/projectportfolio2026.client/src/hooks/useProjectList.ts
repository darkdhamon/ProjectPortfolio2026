import { startTransition, useDeferredValue, useEffect, useRef, useState } from 'react';
import { fetchJsonWithStartupRetry } from '../app/api';
import type { NavigateFn } from '../app/navigation';
import type { ProjectListResponse, ProjectSummary } from '../app/types';
import {
    buildListSearch,
    buildProjectsPath,
    createRouteKey,
    mergeProjects,
    type ListFilters
} from '../appSupport';

const pageSize = 6;

interface UseProjectListOptions {
    filters: ListFilters;
    onNavigate: NavigateFn;
}

interface UseProjectListResult {
    searchInput: string;
    selectedSkills: string[];
    projects: ProjectSummary[];
    availableSkills: string[];
    totalCount: number;
    hasMore: boolean;
    isInitialLoad: boolean;
    isLoading: boolean;
    error: string | null;
    listSearch: string;
    sentinelRef: React.RefObject<HTMLDivElement | null>;
    handleSearchChange: (nextValue: string) => void;
    toggleSkill: (skill: string) => void;
    clearFilters: () => void;
}

export function useProjectList({
    filters,
    onNavigate
}: UseProjectListOptions): UseProjectListResult {
    const [searchInput, setSearchInput] = useState(filters.searchInput);
    const deferredSearch = useDeferredValue(searchInput.trim());
    const [selectedSkills, setSelectedSkills] = useState<string[]>(filters.selectedSkills);
    const [projects, setProjects] = useState<ProjectSummary[]>([]);
    const [availableSkills, setAvailableSkills] = useState<string[]>([]);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [hasMore, setHasMore] = useState(false);
    const [isInitialLoad, setIsInitialLoad] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const latestRequestIdRef = useRef<string | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);
    const previousQueryKeyRef = useRef<string | null>(null);
    const routeKey = createRouteKey(filters);
    const previousRouteKeyRef = useRef(routeKey);
    const fetchQueryKey = createRouteKey({
        searchInput: deferredSearch,
        selectedSkills
    });
    const listSearch = buildListSearch({
        searchInput,
        selectedSkills
    });

    useEffect(() => {
        if (previousRouteKeyRef.current === routeKey) {
            return;
        }

        previousRouteKeyRef.current = routeKey;
        setSearchInput(filters.searchInput);
        setSelectedSkills(filters.selectedSkills);
        setPage(1);
        setProjects([]);
        setAvailableSkills([]);
        setTotalCount(0);
        setHasMore(false);
        setError(null);
        setIsInitialLoad(true);
    }, [filters.searchInput, filters.selectedSkills, routeKey]);

    useEffect(() => {
        const nextPath = buildProjectsPath(listSearch);
        const currentPath = `${window.location.pathname}${window.location.search}`;

        if (currentPath !== nextPath) {
            onNavigate(nextPath, { replace: true, preserveScroll: true });
        }
    }, [listSearch, onNavigate]);

    useEffect(() => {
        const requestId = crypto.randomUUID();
        const controller = new AbortController();
        const isLoadingMore = page > 1 && previousQueryKeyRef.current === fetchQueryKey;
        const queryKeyChanged = previousQueryKeyRef.current !== fetchQueryKey;

        latestRequestIdRef.current = requestId;
        setIsLoading(true);
        setError(null);

        void loadProjects();

        return () => controller.abort();

        async function loadProjects() {
            try {
                const responses = queryKeyChanged
                    ? await loadPages(1, page)
                    : [await loadSinglePage(page)];

                if (latestRequestIdRef.current !== requestId || responses.length === 0) {
                    return;
                }

                startTransition(() => {
                    const latestResponse = responses[responses.length - 1];
                    const incomingProjects = responses.flatMap(response => response.items);

                    setProjects(currentProjects => isLoadingMore
                        ? mergeProjects(currentProjects, incomingProjects)
                        : mergeProjects([], incomingProjects));
                    setAvailableSkills(latestResponse.availableSkills);
                    setTotalCount(latestResponse.totalCount);
                    setHasMore(latestResponse.hasMore);
                });

                previousQueryKeyRef.current = fetchQueryKey;
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError' || latestRequestIdRef.current !== requestId) {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load projects right now.');
                if (!isLoadingMore) {
                    setProjects([]);
                    setAvailableSkills([]);
                    setTotalCount(0);
                    setHasMore(false);
                }
            } finally {
                if (latestRequestIdRef.current === requestId) {
                    setIsInitialLoad(false);
                    setIsLoading(false);
                }
            }
        }

        async function loadPages(startPage: number, endPage: number) {
            const pages: ProjectListResponse[] = [];

            for (let currentPage = startPage; currentPage <= endPage; currentPage += 1) {
                pages.push(await loadSinglePage(currentPage));
            }

            return pages;
        }

        async function loadSinglePage(currentPage: number) {
            const query = new URLSearchParams({
                page: currentPage.toString(),
                pageSize: pageSize.toString(),
                requestId
            });

            if (deferredSearch.length > 0) {
                query.set('search', deferredSearch);
            }

            if (selectedSkills.length > 0) {
                query.set('skills', selectedSkills.join(','));
            }

            const listResponse = await fetchJsonWithStartupRetry<ProjectListResponse>(
                `/api/projects?${query.toString()}`,
                {
                    signal: controller.signal
                },
                'Unable to load projects right now.'
            );

            if (listResponse.requestId !== requestId) {
                throw new DOMException('Stale response.', 'AbortError');
            }

            return listResponse;
        }
    }, [deferredSearch, fetchQueryKey, page, selectedSkills]);

    useEffect(() => {
        const sentinel = sentinelRef.current;
        if (!sentinel || !hasMore || isLoading) {
            return;
        }

        const observer = new IntersectionObserver(entries => {
            if (entries.some(entry => entry.isIntersecting)) {
                setPage(currentPage => currentPage + 1);
            }
        }, { rootMargin: '240px 0px' });

        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [hasMore, isLoading, projects.length]);

    function handleSearchChange(nextValue: string) {
        setSearchInput(nextValue);
        setPage(1);
    }

    function toggleSkill(skill: string) {
        setSelectedSkills(currentSkills =>
            currentSkills.includes(skill)
                ? currentSkills.filter(currentSkill => currentSkill !== skill)
                : [...currentSkills, skill]);
        setPage(1);
    }

    function clearFilters() {
        setSearchInput('');
        setSelectedSkills([]);
        setPage(1);
    }

    return {
        searchInput,
        selectedSkills,
        projects,
        availableSkills,
        totalCount,
        hasMore,
        isInitialLoad,
        isLoading,
        error,
        listSearch,
        sentinelRef,
        handleSearchChange,
        toggleSkill,
        clearFilters
    };
}
