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
    const response = await fetchJsonWithStartupRetry<ProjectListResponse>(
        `/api/admin/projects?page=1&pageSize=50&requestId=${requestId}`,
        {
            signal: signal ?? new AbortController().signal
        },
        'Unable to load projects right now.'
    );

    return response.items;
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
