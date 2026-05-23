import { cleanup, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Employer, PortfolioProfile } from '../../app/types';
import { ResumePage } from './ResumePage';

vi.mock('../../hooks/usePortfolioProfile', () => ({
    usePortfolioProfile: vi.fn()
}));

vi.mock('../../hooks/useWorkHistory', () => ({
    useWorkHistory: vi.fn()
}));

import { usePortfolioProfile } from '../../hooks/usePortfolioProfile';
import { useWorkHistory } from '../../hooks/useWorkHistory';

const mockUsePortfolioProfile = vi.mocked(usePortfolioProfile);
const mockUseWorkHistory = vi.mocked(useWorkHistory);

function createProfile(overrides: Partial<PortfolioProfile> = {}): PortfolioProfile {
    return {
        id: 1,
        displayName: 'Bronze Loft',
        contactHeadline: 'Reach out.',
        contactIntro: 'Profile summary.',
        availabilityHeadline: 'Available now',
        availabilitySummary: 'Ready for the next role.',
        contactMethods: [],
        socialLinks: [],
        ...overrides
    };
}

function createEmployer(id: number, overrides: Partial<Employer> = {}): Employer {
    return {
        id,
        name: `Employer ${id}`,
        city: 'Chicago',
        region: 'IL',
        jobRoles: [
            {
                role: `Role ${id}`,
                startDate: '2024-01-08',
                endDate: null,
                descriptionMarkdown: 'Built and shipped product work.',
                skills: [],
                technologies: []
            }
        ],
        ...overrides
    };
}

describe('ResumePage', () => {
    afterEach(() => {
        cleanup();
    });

    beforeEach(() => {
        mockUsePortfolioProfile.mockReturnValue({
            profile: createProfile(),
            isLoading: false,
            error: null,
            isMissing: false
        });

        mockUseWorkHistory.mockReturnValue({
            employers: [createEmployer(1)],
            isLoading: false,
            error: null
        });
    });

    it('derives contact links and leaves unsupported values as plain content', () => {
        mockUsePortfolioProfile.mockReturnValue({
            profile: createProfile({
                contactMethods: [
                    {
                        type: 'email',
                        label: 'Email',
                        value: 'bronze@example.dev',
                        sortOrder: 1
                    },
                    {
                        type: 'website',
                        label: 'Portfolio',
                        value: 'https://portfolio.example.dev',
                        sortOrder: 2
                    },
                    {
                        type: 'other',
                        label: 'Signal',
                        value: 'signal:bronze-loft',
                        note: 'Shared on request.',
                        sortOrder: 3
                    }
                ],
                socialLinks: []
            }),
            isLoading: false,
            error: null,
            isMissing: false
        });

        render(<ResumePage />);

        expect(screen.getByRole('link', { name: /Email.*bronze@example\.dev/i })).toHaveAttribute('href', 'mailto:bronze@example.dev');
        expect(screen.getByRole('link', { name: /Portfolio.*https:\/\/portfolio\.example\.dev/i })).toHaveAttribute('href', 'https://portfolio.example.dev');

        const signalCard = screen.getByText('Signal').closest('.resume-link-card');
        expect(signalCard).not.toBeNull();
        expect(within(signalCard as HTMLElement).getByText('signal:bronze-loft')).toBeInTheDocument();
        expect(within(signalCard as HTMLElement).getByText('Shared on request.')).toBeInTheDocument();
        expect(within(signalCard as HTMLElement).queryByRole('link')).not.toBeInTheDocument();
    });

    it('shows empty-state copy and missing-config action state when public data is unavailable', () => {
        mockUsePortfolioProfile.mockReturnValue({
            profile: null,
            isLoading: false,
            error: null,
            isMissing: true
        });

        mockUseWorkHistory.mockReturnValue({
            employers: [],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        expect(screen.getByRole('heading', { name: 'Resume shell ready for public portfolio data.' })).toBeInTheDocument();
        expect(screen.getByText('Waiting for published resume data')).toBeInTheDocument();
        expect(screen.getByText('Needs config')).toBeInTheDocument();
        expect(screen.getByText('Public contact and social links will appear here once the portfolio profile is configured.')).toBeInTheDocument();
        expect(screen.getByText('Published work history will appear here once employer and job-role records are available.')).toBeInTheDocument();
    });

    it('limits the experience shell to the first three employers', () => {
        mockUseWorkHistory.mockReturnValue({
            employers: [createEmployer(1), createEmployer(2), createEmployer(3), createEmployer(4)],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        const experiencePanel = screen.getByText('Experience Shell').closest('article');
        expect(experiencePanel).not.toBeNull();

        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 1' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 2' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 3' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).queryByRole('heading', { name: 'Employer 4' })).not.toBeInTheDocument();
    });
});
