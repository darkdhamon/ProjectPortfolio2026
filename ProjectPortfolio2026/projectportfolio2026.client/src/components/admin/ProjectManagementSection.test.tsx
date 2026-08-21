import { render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectSummary } from '../../app/types';
import { ProjectManagementSection } from './ProjectManagementSection';

const fetchAdminProjects = vi.fn<() => Promise<ProjectSummary[]>>();

vi.mock('../../api/adminProjects', () => ({
    fetchAdminProjects: () => fetchAdminProjects(),
    setFeaturedProjectOrder: vi.fn(),
    setProjectArchivedState: vi.fn(),
    setProjectFeaturedState: vi.fn()
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
});
