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
        contactHeadline: 'Full-stack engineer focused on dependable delivery.',
        contactIntro: 'I build portfolio, product, and platform experiences that stay usable under real operating pressure.',
        availabilityHeadline: 'Open to senior product engineering roles',
        availabilitySummary: 'Remote-friendly, collaborative, and comfortable owning delivery from API to UI polish.',
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
                descriptionMarkdown: 'Built and shipped product work.\n\nPartnered with stakeholders to keep the roadmap moving.',
                skills: [`Skill ${id}`],
                technologies: [`Technology ${id}`]
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

    it('shows empty-state copy and hides optional sections when public data is unavailable', () => {
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
        expect(screen.queryByRole('heading', { name: 'Positioning for recruiters and hiring teams.' })).not.toBeInTheDocument();
        expect(screen.queryByRole('heading', { name: 'Core skills and technologies.' })).not.toBeInTheDocument();
    });

    it('renders structured summary, skill highlights, and full experience history', () => {
        mockUseWorkHistory.mockReturnValue({
            employers: [
                createEmployer(1),
                createEmployer(2, {
                    city: 'Austin',
                    region: 'TX',
                    jobRoles: [
                        {
                            role: 'Principal Engineer',
                            startDate: '2021-05-03',
                            endDate: '2023-12-15',
                            descriptionMarkdown: 'Directed architecture strategy and delivery planning.',
                            skills: ['Architecture', 'Leadership'],
                            technologies: ['React', '.NET 10']
                        },
                        {
                            role: 'Senior Engineer',
                            startDate: '2019-02-11',
                            endDate: '2021-05-01',
                            descriptionMarkdown: 'Led service migrations and performance work.',
                            skills: ['Performance'],
                            technologies: ['SQL Server']
                        }
                    ]
                }),
                createEmployer(3),
                createEmployer(4)
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        const summaryPanel = screen.getByRole('heading', { name: 'Positioning for recruiters and hiring teams.' }).closest('article');
        expect(summaryPanel).not.toBeNull();
        expect(within(summaryPanel as HTMLElement).getByText('Full-stack engineer focused on dependable delivery.')).toBeInTheDocument();
        expect(within(summaryPanel as HTMLElement).getByText('Remote-friendly, collaborative, and comfortable owning delivery from API to UI polish.')).toBeInTheDocument();

        expect(screen.getByRole('heading', { name: 'Core skills and technologies.' })).toBeInTheDocument();
        expect(screen.getByLabelText('Resume skills')).toHaveTextContent('Skill 1');
        expect(screen.getByLabelText('Resume skills')).toHaveTextContent('Architecture');
        expect(screen.getByLabelText('Resume technologies')).toHaveTextContent('.NET 10');
        expect(screen.getByLabelText('Resume technologies')).toHaveTextContent('React');

        const experiencePanel = screen.getByRole('heading', { name: 'Condensed work history.' }).closest('article');
        expect(experiencePanel).not.toBeNull();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 1' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 2' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 3' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 4' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByText('Directed architecture strategy and delivery planning.')).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByLabelText('Principal Engineer skills')).toHaveTextContent('Architecture');
        expect(within(experiencePanel as HTMLElement).getByLabelText('Principal Engineer technologies')).toHaveTextContent('.NET 10');
    });
});
