import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectSummary } from '../../app/types';
import { ProjectManagementSection } from './ProjectManagementSection';

const fetchAdminProjects = vi.fn<() => Promise<ProjectSummary[]>>();
const setFeaturedProjectOrder = vi.fn<(projectIds: number[]) => Promise<void>>();
const setProjectFeaturedState = vi.fn<(projectId: number, isFeatured: boolean) => Promise<ProjectSummary>>();

vi.mock('../../api/adminProjects', () => ({
    fetchAdminProjects: () => fetchAdminProjects(),
    setFeaturedProjectOrder: (projectIds: number[]) => setFeaturedProjectOrder(projectIds),
    setProjectArchivedState: vi.fn(),
    setProjectFeaturedState: (projectId: number, isFeatured: boolean) => setProjectFeaturedState(projectId, isFeatured)
}));

function buildProject(overrides: Partial<ProjectSummary>): ProjectSummary {
    return {
        id: 1,
        title: 'Project',
        startDate: '2026-01-01',
        shortDescription: 'Project summary.',
        isPublished: true,
        isFeatured: false,
        skills: [],
        technologies: [],
        ...overrides
    };
}

describe('ProjectManagementSection', () => {
    beforeEach(() => {
        fetchAdminProjects.mockReset();
        setFeaturedProjectOrder.mockReset();
        setProjectFeaturedState.mockReset();
    });

    afterEach(() => {
        cleanup();
    });

    it('separates unpublished drafts from featured and available project controls', async () => {
        fetchAdminProjects.mockResolvedValue([
            buildProject({ id: 1, title: 'Published featured', isFeatured: true }),
            buildProject({ id: 2, title: 'Published available' }),
            buildProject({ id: 3, title: 'Unpublished draft', isPublished: false, isFeatured: true })
        ]);

        render(<ProjectManagementSection />);

        const draftHeading = await screen.findByRole('heading', { name: 'Not published' });
        const draftCard = draftHeading.closest('article');

        expect(draftCard).not.toBeNull();
        expect(within(draftCard!).getByText('Unpublished draft')).toBeInTheDocument();
        expect(within(draftCard!).getByText('Draft - not visible on public project surfaces.')).toBeInTheDocument();
        expect(within(draftCard!).queryByRole('button', { name: 'Feature' })).not.toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Feature' })).toBeInTheDocument();
    });

    it('features and unfeatures published projects with refreshed state', async () => {
        fetchAdminProjects.mockResolvedValue([
            buildProject({ id: 1, title: 'Featured project', isFeatured: true }),
            buildProject({ id: 2, title: 'Available project' })
        ]);
        setProjectFeaturedState.mockResolvedValue(buildProject({ id: 2, title: 'Available project', isFeatured: true }));
        render(<ProjectManagementSection />);

        const availableRow = (await screen.findByText('Available project')).closest('li');
        fireEvent.click(within(availableRow!).getByRole('button', { name: 'Feature' }));
        await waitFor(() => expect(setProjectFeaturedState).toHaveBeenCalledWith(2, true));

        const featuredRow = screen.getByText('Featured project').closest('li');
        fireEvent.click(within(featuredRow!).getByRole('button', { name: 'Unfeature' }));
        await waitFor(() => expect(setProjectFeaturedState).toHaveBeenCalledWith(1, false));
    });

    it('submits a complete reordered featured snapshot', async () => {
        fetchAdminProjects.mockResolvedValue([
            buildProject({ id: 1, title: 'First', isFeatured: true, featuredOrder: 0 }),
            buildProject({ id: 2, title: 'Second', isFeatured: true, featuredOrder: 1 })
        ]);
        setFeaturedProjectOrder.mockResolvedValue();
        render(<ProjectManagementSection />);

        const firstRow = (await screen.findByText('First')).closest('li');
        fireEvent.click(within(firstRow!).getByRole('button', { name: 'Move down' }));

        await waitFor(() => expect(setFeaturedProjectOrder).toHaveBeenCalledWith([2, 1]));
    });

    it('does not show mutation success when the refresh fails', async () => {
        fetchAdminProjects
            .mockResolvedValueOnce([buildProject({ id: 2, title: 'Available project' })])
            .mockRejectedValueOnce(new Error('Refresh failed.'));
        setProjectFeaturedState.mockResolvedValue(buildProject({ id: 2, title: 'Available project', isFeatured: true }));
        render(<ProjectManagementSection />);

        const availableRow = (await screen.findByText('Available project')).closest('li');
        fireEvent.click(within(availableRow!).getByRole('button', { name: 'Feature' }));

        expect(await screen.findByText('Refresh failed.')).toBeInTheDocument();
        expect(screen.queryByText('Project was marked as featured.')).not.toBeInTheDocument();
    });
});
