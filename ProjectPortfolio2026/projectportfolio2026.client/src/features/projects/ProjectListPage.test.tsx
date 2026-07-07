import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProjectListPage } from './ProjectListPage';
import { useProjectList } from '../../hooks/useProjectList';
import type { ProjectSummary } from '../../app/types';

vi.mock('../../hooks/useProjectList', () => ({
    useProjectList: vi.fn()
}));

const useProjectListMock = vi.mocked(useProjectList);

describe('ProjectListPage', () => {
    beforeEach(() => {
        useProjectListMock.mockReset();
    });

    it('shows filtered result counts and available skill totals in the collapsed skill tray', () => {
        useProjectListMock.mockReturnValue(buildHookState({
            projects: [buildProject(42, 'Portfolio Refresh')],
            availableSkills: ['React', 'Testing'],
            totalCount: 3
        }));

        renderProjectListPage();

        expect(screen.getByText('Showing 1 of 3 published projects')).toBeInTheDocument();
        expect(screen.getAllByText('No active filters')).toHaveLength(2);

        fireEvent.click(screen.getByRole('button', { name: 'Hide skills' }));

        expect(screen.getByText('2 available skills')).toBeInTheDocument();
    });

    it('counts trimmed search text with selected skills and keeps that summary when skills are hidden', () => {
        useProjectListMock.mockReturnValue(buildHookState({
            searchInput: '  React  ',
            selectedSkills: ['React', 'Testing'],
            availableSkills: ['React', 'Testing', 'TypeScript']
        }));

        renderProjectListPage();

        expect(screen.getAllByText('3 active filters')).toHaveLength(2);

        fireEvent.click(screen.getByRole('button', { name: 'Hide skills' }));

        expect(screen.getByText('2 selected skills')).toBeInTheDocument();
    });
});

function renderProjectListPage() {
    render(
        <ProjectListPage
            filters={{
                searchInput: '',
                selectedSkills: []
            }}
            onNavigate={vi.fn()}
        />
    );
}

function buildHookState(overrides: Partial<ReturnType<typeof useProjectList>> = {}): ReturnType<typeof useProjectList> {
    return {
        searchInput: '',
        selectedSkills: [],
        projects: [],
        availableSkills: [],
        totalCount: 0,
        hasMore: false,
        isInitialLoad: false,
        isLoading: false,
        error: null,
        listSearch: '',
        sentinelRef: { current: null },
        handleSearchChange: vi.fn(),
        toggleSkill: vi.fn(),
        clearFilters: vi.fn(),
        ...overrides
    };
}

function buildProject(id: number, title: string): ProjectSummary {
    return {
        id,
        title,
        startDate: '2025-01-01',
        endDate: null,
        primaryImageUrl: null,
        shortDescription: `${title} summary`,
        isFeatured: false,
        skills: ['React'],
        technologies: ['TypeScript']
    };
}
