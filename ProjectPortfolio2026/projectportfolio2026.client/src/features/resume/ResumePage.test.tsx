import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
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
        vi.useRealTimers();
    });

    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-05-24T12:00:00Z'));
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
        expect(screen.queryByRole('heading', { name: 'Highlighted skills and technologies.' })).not.toBeInTheDocument();
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

        expect(screen.getByRole('heading', { name: 'Highlighted skills and technologies.' })).toBeInTheDocument();
        expect(screen.getByLabelText('Resume skills')).toHaveTextContent('Skill 1');
        expect(screen.getByLabelText('Resume skills')).toHaveTextContent('Architecture');
        expect(screen.getByLabelText('Resume technologies')).toHaveTextContent('.NET 10');
        expect(screen.getByLabelText('Resume technologies')).toHaveTextContent('React');
        expect(screen.getAllByText('Appears in 1 visible role.').length).toBeGreaterThan(0);

        const experiencePanel = screen.getByRole('heading', { name: 'Condensed work history.' }).closest('article');
        expect(experiencePanel).not.toBeNull();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 1' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 2' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 3' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByRole('heading', { name: 'Employer 4' })).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByText('Directed architecture strategy and delivery planning.')).toBeInTheDocument();
        expect(within(experiencePanel as HTMLElement).getByLabelText('Principal Engineer skills')).toHaveTextContent('Architecture');
        expect(within(experiencePanel as HTMLElement).getByLabelText('Principal Engineer technologies')).toHaveTextContent('.NET 10');
        expect(within(experiencePanel as HTMLElement).getByText('Feb 2019 to Dec 2023')).toHaveAttribute('title', 'Time at Employer 2: 4 years, 11 months');
        expect(within(experiencePanel as HTMLElement).getByText('May 2021 to Dec 2023')).toHaveAttribute('title', 'Time in role: 2 years, 8 months');
        expect(within(experiencePanel as HTMLElement).getByText('Feb 2019 to May 2021')).toHaveAttribute('title', 'Time in role: 2 years, 4 months');
    });

    it('filters the resume by time window and recent employer count', () => {
        mockUseWorkHistory.mockReturnValue({
            employers: [
                createEmployer(1, {
                    name: 'Current Employer',
                    jobRoles: [
                        {
                            role: 'Staff Engineer',
                            startDate: '2024-02-01',
                            endDate: null,
                            descriptionMarkdown: 'Owning platform and delivery work.',
                            skills: ['Leadership'],
                            technologies: ['React']
                        }
                    ]
                }),
                createEmployer(2, {
                    name: 'Recent Employer',
                    jobRoles: [
                        {
                            role: 'Senior Engineer',
                            startDate: '2022-03-01',
                            endDate: '2024-01-15',
                            descriptionMarkdown: 'Drove modernization initiatives.',
                            skills: ['Architecture'],
                            technologies: ['.NET 10']
                        }
                    ]
                }),
                createEmployer(3, {
                    name: 'Bridge Employer',
                    jobRoles: [
                        {
                            role: 'Engineer',
                            startDate: '2020-01-01',
                            endDate: '2021-08-15',
                            descriptionMarkdown: 'Supported platform transitions.',
                            skills: ['Testing'],
                            technologies: ['Azure']
                        }
                    ]
                }),
                createEmployer(4, {
                    name: 'Legacy Employer',
                    jobRoles: [
                        {
                            role: 'Analyst',
                            startDate: '2015-01-01',
                            endDate: '2017-12-20',
                            descriptionMarkdown: 'Early delivery work.',
                            skills: ['Support'],
                            technologies: ['SQL Server']
                        }
                    ]
                })
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        expect(screen.getByText('Current Employer')).toBeInTheDocument();
        expect(screen.getByText('Recent Employer')).toBeInTheDocument();
        expect(screen.getByText('Bridge Employer')).toBeInTheDocument();
        expect(screen.getByText('Legacy Employer')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Last 5 years' }));

        expect(screen.getByText('Current Employer')).toBeInTheDocument();
        expect(screen.getByText('Recent Employer')).toBeInTheDocument();
        expect(screen.getByText('Bridge Employer')).toBeInTheDocument();
        expect(screen.queryByText('Legacy Employer')).not.toBeInTheDocument();
        expect(screen.getByText('3 roles across 3 employers')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Top 3' }));

        expect(screen.getByRole('button', { name: 'Top 3' })).toHaveAttribute('aria-pressed', 'true');
        expect(screen.getByText('Visible Employers')).toBeInTheDocument();
        expect(screen.getByText('Active Filters')).toBeInTheDocument();
    });

    it('avoids duplicate key warnings when role titles and start dates repeat', () => {
        const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

        mockUseWorkHistory.mockReturnValue({
            employers: [
                createEmployer(1, {
                    jobRoles: [
                        {
                            role: 'Software Engineer',
                            startDate: '2022-01-10',
                            endDate: '2022-12-20',
                            descriptionMarkdown: 'Supported release work.',
                            skills: ['Testing'],
                            technologies: ['React']
                        },
                        {
                            role: 'Software Engineer',
                            startDate: '2022-01-10',
                            endDate: '2023-06-15',
                            descriptionMarkdown: 'Expanded platform ownership.',
                            skills: ['Architecture'],
                            technologies: ['.NET 10']
                        }
                    ]
                })
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        expect(consoleErrorSpy).not.toHaveBeenCalledWith(expect.stringContaining('Encountered two children with the same key'));
        consoleErrorSpy.mockRestore();
    });
});
