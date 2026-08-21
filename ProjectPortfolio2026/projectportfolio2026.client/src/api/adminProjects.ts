import { fetchAuthJson, fetchJsonWithStartupRetry } from './http';
import type { ProjectListResponse, ProjectSummary } from '../app/types';

export interface ProjectFeaturedStateRequest {
    isFeatured: boolean;
}

export interface ProjectFeaturedOrderRequest {
    projectIds: number[];
}

export async function fetchAdminProjects(signal?: AbortSignal): Promise<ProjectSummary[]> {
    const requestId = crypto.randomUUID();
    const requestSignal = signal ?? new AbortController().signal;
    const projects: ProjectSummary[] = [];
    let page = 1;
    let hasMore = true;

    while (hasMore) {
        const response = await fetchJsonWithStartupRetry<ProjectListResponse>(
            `/api/admin/projects?page=${page}&pageSize=50&requestId=${requestId}`,
            { signal: requestSignal },
            'Unable to load projects right now.'
        );

        projects.push(...response.items);
        hasMore = response.hasMore;
        page += 1;
    }

    return projects;
}

export async function setProjectFeaturedState(projectId: number, isFeatured: boolean): Promise<ProjectSummary> {
    const { payload } = await fetchAuthJson<ProjectSummary>(
        `/api/admin/projects/${projectId}/featured-state`,
        {
            method: 'PUT',
            body: JSON.stringify({ isFeatured })
        },
        'Unable to update featured project state.'
    );

    return payload as ProjectSummary;
}

export async function setFeaturedProjectOrder(projectIds: number[]): Promise<void> {
    await fetchAuthJson<unknown>(
        '/api/admin/projects/featured-order',
        {
            method: 'PUT',
            body: JSON.stringify({ projectIds } as ProjectFeaturedOrderRequest)
        },
        'Unable to save featured project order.'
    );
}

export async function setProjectArchivedState(projectId: number, isArchived: boolean): Promise<void> {
    await fetchAuthJson<unknown>(
        `/api/admin/projects/${projectId}/${isArchived ? 'archive' : 'restore'}`,
        { method: 'PUT' },
        `Unable to ${isArchived ? 'archive' : 'restore'} project.`
    );
}
