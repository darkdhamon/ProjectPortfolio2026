import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectSummary } from '../../app/types';
import { ProjectManagementSection } from './ProjectManagementSection';

vi.mock('../../api/adminProjects', () => ({
    fetchAdminProjects: vi.fn(),
    setFeaturedProjectOrder: vi.fn(),
    setProjectFeaturedState: vi.fn()
}));

import {
    fetchAdminProjects,
    setFeaturedProjectOrder,
    setProjectFeaturedState
} from '../../api/adminProjects';

const mockFetchAdminProjects = vi.mocked(fetchAdminProjects);
const mockSetFeaturedProjectOrder = vi.mocked(setFeaturedProjectOrder);
const mockSetProjectFeaturedState = vi.mocked(setProjectFeaturedState);

describe('ProjectManagementSection', () => {
    beforeEach(() => {
        mockFetchAdminProjects.mockResolvedValue([
            project({ id: 1, title: 'Featured Alpha', isFeatured: true, featuredOrder: 0 }),
            project({ id: 2, title: 'Published Candidate' }),
            project({ id: 3, title: 'Draft Candidate', isPublished: false })
        ]);
    });

    afterEach(() => {
        cleanup();
        vi.clearAllMocks();
    });

    it('loads the complete inventory and prevents featuring drafts', async () => {
        render(<ProjectManagementSection />);

        expect(await screen.findByText('Featured Alpha')).toBeInTheDocument();
        const draftRow = screen.getByText('Draft Candidate').closest('li');

        expect(draftRow).not.toBeNull();
        expect(within(draftRow!).getByText('Draft — publish this project before featuring it.')).toBeInTheDocument();
        expect(within(draftRow!).getByRole('button', { name: 'Feature' })).toBeDisabled();
    });

    it('features a published project and refreshes the inventory', async () => {
        mockSetProjectFeaturedState.mockResolvedValue(project({ id: 2, title: 'Published Candidate', isFeatured: true, featuredOrder: 1 }));
        render(<ProjectManagementSection />);

        const candidateRow = (await screen.findByText('Published Candidate')).closest('li');
        fireEvent.click(within(candidateRow!).getByRole('button', { name: 'Feature' }));

        await waitFor(() => expect(mockSetProjectFeaturedState).toHaveBeenCalledWith(2, true));
        expect(mockFetchAdminProjects).toHaveBeenCalledTimes(2);
        expect(await screen.findByText('Project was marked as featured.')).toBeInTheDocument();
    });

    it('unfeatures an existing featured project and refreshes the inventory', async () => {
        mockSetProjectFeaturedState.mockResolvedValue(project({ id: 1, title: 'Featured Alpha' }));
        render(<ProjectManagementSection />);

        const featuredRow = (await screen.findByText('Featured Alpha')).closest('li');
        fireEvent.click(within(featuredRow!).getByRole('button', { name: 'Unfeature' }));

        await waitFor(() => expect(mockSetProjectFeaturedState).toHaveBeenCalledWith(1, false));
        expect(mockFetchAdminProjects).toHaveBeenCalledTimes(2);
        expect(await screen.findByText('Project was removed from featured projects.')).toBeInTheDocument();
    });

    it('submits the reordered featured snapshot and refreshes it', async () => {
        mockFetchAdminProjects.mockResolvedValue([
            project({ id: 1, title: 'Featured Alpha', isFeatured: true, featuredOrder: 0 }),
            project({ id: 2, title: 'Featured Beta', isFeatured: true, featuredOrder: 1 })
        ]);
        mockSetFeaturedProjectOrder.mockResolvedValue();
        render(<ProjectManagementSection />);

        const alphaRow = (await screen.findByText('Featured Alpha')).closest('li');
        fireEvent.click(within(alphaRow!).getByRole('button', { name: 'Move down' }));

        await waitFor(() => expect(mockSetFeaturedProjectOrder).toHaveBeenCalledWith([2, 1]));
        expect(mockFetchAdminProjects).toHaveBeenCalledTimes(2);
        expect(await screen.findByText('Featured project order updated.')).toBeInTheDocument();
    });

    it('reports a refresh failure without showing mutation success', async () => {
        mockFetchAdminProjects
            .mockResolvedValueOnce([project({ id: 2, title: 'Published Candidate' })])
            .mockRejectedValueOnce(new Error('Refresh failed.'));
        mockSetProjectFeaturedState.mockResolvedValue(project({ id: 2, title: 'Published Candidate', isFeatured: true }));
        render(<ProjectManagementSection />);

        const candidateRow = (await screen.findByText('Published Candidate')).closest('li');
        fireEvent.click(within(candidateRow!).getByRole('button', { name: 'Feature' }));

        expect(await screen.findByText('Refresh failed.')).toBeInTheDocument();
        expect(screen.queryByText('Project was marked as featured.')).not.toBeInTheDocument();
    });
});

function project(overrides: Partial<ProjectSummary>): ProjectSummary {
    return {
        id: 100,
        title: 'Project',
        startDate: '2026-01-01',
        endDate: null,
        primaryImageUrl: null,
        shortDescription: 'Project summary.',
        gitHubUrl: null,
        demoUrl: null,
        isPublished: true,
        isFeatured: false,
        featuredOrder: null,
        skills: [],
        technologies: [],
        ...overrides
    };
}
