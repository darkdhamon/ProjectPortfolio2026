/// <reference types="node" />
import { readFileSync } from 'fs';
import { resolve } from 'path';
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Employer, PortfolioProfile, ResumeConfiguration } from '../../app/types';
import { ResumePage } from './ResumePage';

vi.mock('../../hooks/usePortfolioProfile', () => ({
    usePortfolioProfile: vi.fn()
}));

vi.mock('../../hooks/useWorkHistory', () => ({
    useWorkHistory: vi.fn()
}));

vi.mock('../../hooks/useResumeConfiguration', () => ({
    useResumeConfiguration: vi.fn()
}));

import { usePortfolioProfile } from '../../hooks/usePortfolioProfile';
import { useResumeConfiguration } from '../../hooks/useResumeConfiguration';
import { useWorkHistory } from '../../hooks/useWorkHistory';

const mockUsePortfolioProfile = vi.mocked(usePortfolioProfile);
const mockUseResumeConfiguration = vi.mocked(useResumeConfiguration);
const mockUseWorkHistory = vi.mocked(useWorkHistory);
const originalCreateObjectUrlDescriptor = Object.getOwnPropertyDescriptor(URL, 'createObjectURL');
const originalRevokeObjectUrlDescriptor = Object.getOwnPropertyDescriptor(URL, 'revokeObjectURL');
const unicodePdfFontBytes = readFileSync(resolve(process.cwd(), 'node_modules', '@fontpkg', 'unifont', 'unifont-15.0.01.ttf'));

function restoreUrlProperty(name: 'createObjectURL' | 'revokeObjectURL', descriptor?: PropertyDescriptor) {
    if (descriptor) {
        Object.defineProperty(URL, name, descriptor);
        return;
    }

    Reflect.deleteProperty(URL, name);
}

function mockPdfFontFetch() {
    const fontBytes = unicodePdfFontBytes;
    const fontArrayBuffer = fontBytes.buffer.slice(fontBytes.byteOffset, fontBytes.byteOffset + fontBytes.byteLength);
    const fetchMock = vi.fn().mockResolvedValue({
        ok: true,
        arrayBuffer: async () => fontArrayBuffer.slice(0)
    });
    vi.stubGlobal('fetch', fetchMock);
    return fetchMock;
}

function mockPdfDownloadSupport() {
    const createObjectUrl = vi.fn<(object: Blob) => string>(() => 'blob:resume');
    const revokeObjectUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', {
        configurable: true,
        writable: true,
        value: createObjectUrl
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
        configurable: true,
        writable: true,
        value: revokeObjectUrl
    });
    const anchorClickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

    return {
        anchorClickSpy,
        createObjectUrl,
        fetchMock: mockPdfFontFetch(),
        revokeObjectUrl
    };
}

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

function createResumeConfiguration(overrides: Partial<ResumeConfiguration> = {}): ResumeConfiguration {
    return {
        id: 1,
        sourceType: 'hosted-file',
        sourceUrl: 'https://cdn.example.dev/resume.pdf',
        displayLabel: 'Download Resume',
        summary: 'ATS-friendly PDF.',
        isConfigured: true,
        ...overrides
    };
}

describe('ResumePage', () => {
    afterEach(() => {
        cleanup();
        vi.useRealTimers();
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
        restoreUrlProperty('createObjectURL', originalCreateObjectUrlDescriptor);
        restoreUrlProperty('revokeObjectURL', originalRevokeObjectUrlDescriptor);
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
        mockUseResumeConfiguration.mockReturnValue({
            configuration: createResumeConfiguration(),
            isLoading: false,
            error: null,
            isMissing: false
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
                socialLinks: [
                    {
                        platform: 'github',
                        label: 'GitHub',
                        url: 'https://github.com/darkdhamon',
                        summary: 'Implementation history and public code.',
                        sortOrder: 1
                    }
                ]
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
        expect(screen.getByRole('link', { name: /GitHub.*GitHub/i })).toHaveAttribute('href', 'https://github.com/darkdhamon');
        expect(screen.getByText('Implementation history and public code.')).toBeInTheDocument();
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
        mockUseResumeConfiguration.mockReturnValue({
            configuration: null,
            isLoading: false,
            error: null,
            isMissing: true
        });

        render(<ResumePage />);

        expect(screen.getByRole('heading', { name: 'Resume shell ready for public portfolio data.' })).toBeInTheDocument();
        expect(screen.getByText('Waiting for published resume data')).toBeInTheDocument();
        expect(screen.getByText('Public contact and social links will appear here once the portfolio profile is configured.')).toBeInTheDocument();
        expect(screen.getByText('Published work history will appear here once employer and job-role records are available.')).toBeInTheDocument();
        expect(screen.queryByRole('heading', { name: 'Positioning for recruiters and hiring teams.' })).not.toBeInTheDocument();
        expect(screen.queryByRole('heading', { name: 'Highlighted skills and technologies.' })).not.toBeInTheDocument();
    });

    it('renders the configured public resume action when external source settings are available', () => {
        render(<ResumePage />);

        expect(screen.getByRole('link', { name: 'Download Resume' })).toHaveAttribute('href', 'https://cdn.example.dev/resume.pdf');
        expect(screen.getByText('ATS-friendly PDF.')).toBeInTheDocument();
        expect(screen.getAllByText('Hosted file').length).toBeGreaterThan(0);
    });

    it('hides the resume source action when external configuration is incomplete', () => {
        mockUseResumeConfiguration.mockReturnValue({
            configuration: {
                id: 1,
                sourceType: 'none',
                isConfigured: false,
                sourceUrl: null,
                displayLabel: null,
                summary: null
            },
            isLoading: false,
            error: null,
            isMissing: true
        });

        render(<ResumePage />);

        expect(screen.queryByRole('link', { name: 'Download Resume' })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Open Resume Source/i })).not.toBeInTheDocument();
        expect(screen.queryByText('Hosted file')).not.toBeInTheDocument();
        expect(screen.queryByText('Needs config')).not.toBeInTheDocument();
    });

    it('exports the currently filtered resume view as a PDF download', async () => {
        vi.useRealTimers();
        const { anchorClickSpy, createObjectUrl, revokeObjectUrl } = mockPdfDownloadSupport();

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
        fireEvent.click(screen.getByRole('button', { name: 'Last 5 years' }));
        fireEvent.click(screen.getByRole('button', { name: 'Download PDF' }));

        expect(await screen.findByText('PDF download started.')).toBeInTheDocument();
        expect(createObjectUrl).toHaveBeenCalledTimes(1);
        expect(anchorClickSpy).toHaveBeenCalledTimes(1);
        expect(revokeObjectUrl).toHaveBeenCalledWith('blob:resume');

        const pdfBlob = createObjectUrl.mock.calls[0]?.[0] as unknown as Blob;
        expect(pdfBlob.type).toBe('application/pdf');
        const pdfBytes = new Uint8Array(await pdfBlob.arrayBuffer());
        const pdfHeader = new TextDecoder().decode(pdfBytes.slice(0, 8));
        expect(pdfHeader).toContain('%PDF-');
    });

    it('shows a clean error when the PDF export pipeline is unavailable', () => {
        vi.useRealTimers();
        Object.defineProperty(URL, 'createObjectURL', {
            configurable: true,
            writable: true,
            value: undefined
        });

        render(<ResumePage />);

        fireEvent.click(screen.getByRole('button', { name: 'Download PDF' }));

        return screen.findByText('Unable to export the resume PDF right now.')
            .then(errorBanner => {
                expect(errorBanner).toBeInTheDocument();
            });
    });

    it('keeps PDF export available while resume source configuration is still loading', async () => {
        vi.useRealTimers();
        const { createObjectUrl } = mockPdfDownloadSupport();
        mockUseResumeConfiguration.mockReturnValue({
            configuration: null,
            isLoading: true,
            error: null,
            isMissing: false
        });

        render(<ResumePage />);

        const downloadButton = screen.getByRole('button', { name: 'Download PDF' });
        expect(downloadButton).toBeEnabled();

        fireEvent.click(downloadButton);

        expect(await screen.findByText('PDF download started.')).toBeInTheDocument();
        expect(createObjectUrl).toHaveBeenCalledTimes(1);
    });

    it('keeps PDF export available when resume source configuration fails', async () => {
        vi.useRealTimers();
        const { createObjectUrl } = mockPdfDownloadSupport();
        mockUseResumeConfiguration.mockReturnValue({
            configuration: null,
            isLoading: false,
            error: 'Resume configuration failed.',
            isMissing: false
        });

        render(<ResumePage />);

        expect(screen.getByText('Resume configuration failed.')).toBeInTheDocument();
        const downloadButton = screen.getByRole('button', { name: 'Download PDF' });
        expect(downloadButton).toBeEnabled();

        fireEvent.click(downloadButton);

        expect(await screen.findByText('PDF download started.')).toBeInTheDocument();
        expect(createObjectUrl).toHaveBeenCalledTimes(1);
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
        vi.setSystemTime(new Date('2026-05-24T12:00:00Z'));

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

    it('falls back to overall role and location context when filters hide every visible employer', () => {
        mockUsePortfolioProfile.mockReturnValue({
            profile: createProfile({
                contactHeadline: '',
                availabilityHeadline: '',
                availabilitySummary: ''
            }),
            isLoading: false,
            error: null,
            isMissing: false
        });

        mockUseWorkHistory.mockReturnValue({
            employers: [
                createEmployer(1, {
                    city: 'Madison',
                    region: 'WI',
                    jobRoles: [
                        {
                            role: 'Principal Engineer',
                            startDate: '2016-02-01',
                            endDate: '2018-01-31',
                            descriptionMarkdown: 'Archived platform leadership work.',
                            skills: ['Architecture'],
                            technologies: ['Azure']
                        }
                    ]
                })
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        fireEvent.click(screen.getByRole('button', { name: 'Last 5 years' }));

        expect(screen.getByText(/Principal Engineer based in Madison, WI\./i)).toBeInTheDocument();
        expect(screen.getByText('No roles match the current resume filters')).toBeInTheDocument();
        expect(screen.getByText('No published roles match the active resume filters yet. Expand the time window or increase the employer count to bring older experience back into view.')).toBeInTheDocument();
        expect(screen.getByText('No published roles match the current resume filters. Expand the time window or increase the employer count to bring older experience back into view.')).toBeInTheDocument();
    });

    it('shows loading and error banners from the public resume hooks', () => {
        mockUsePortfolioProfile.mockReturnValue({
            profile: null,
            isLoading: true,
            error: null,
            isMissing: false
        });
        mockUseWorkHistory.mockReturnValue({
            employers: [],
            isLoading: true,
            error: null
        });
        mockUseResumeConfiguration.mockReturnValue({
            configuration: null,
            isLoading: true,
            error: null,
            isMissing: false
        });

        const { rerender } = render(<ResumePage />);

        expect(screen.getByText('Loading resume...')).toBeInTheDocument();

        mockUsePortfolioProfile.mockReturnValue({
            profile: null,
            isLoading: false,
            error: 'Profile request failed.',
            isMissing: false
        });
        mockUseWorkHistory.mockReturnValue({
            employers: [],
            isLoading: false,
            error: 'Work history request failed.'
        });
        mockUseResumeConfiguration.mockReturnValue({
            configuration: null,
            isLoading: false,
            error: 'Resume configuration failed.',
            isMissing: false
        });

        rerender(<ResumePage />);

        expect(screen.queryByText('Loading resume...')).not.toBeInTheDocument();
        expect(screen.getByText('Profile request failed.')).toBeInTheDocument();
        expect(screen.getByText('Work history request failed.')).toBeInTheDocument();
        expect(screen.getByText('Resume configuration failed.')).toBeInTheDocument();
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

    it('uses present-tense duration titles when an employer still has an active role', () => {
        mockUseWorkHistory.mockReturnValue({
            employers: [
                createEmployer(1, {
                    name: 'Northwind Health',
                    city: 'Seattle',
                    region: 'WA',
                    jobRoles: [
                        {
                            role: 'Senior Engineer',
                            startDate: '2022-03-14',
                            endDate: null,
                            descriptionMarkdown: '## Led modernization\n\nDirected the current platform roadmap.',
                            skills: ['Leadership'],
                            technologies: ['TypeScript']
                        },
                        {
                            role: 'Engineer',
                            startDate: '2020-01-06',
                            endDate: '2022-03-01',
                            descriptionMarkdown: 'Built internal APIs.',
                            skills: ['APIs'],
                            technologies: ['.NET 10']
                        }
                    ]
                })
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        const experiencePanel = screen.getByRole('heading', { name: 'Condensed work history.' }).closest('article');
        expect(experiencePanel).not.toBeNull();
        expect(within(experiencePanel as HTMLElement).getByText('Jan 2020 to Present')).toHaveAttribute('title', 'Time at Northwind Health: 6 years, 5 months');
        expect(within(experiencePanel as HTMLElement).getByText('Mar 2022 to Present')).toHaveAttribute('title', 'Time in role: 4 years, 3 months');
        expect(within(experiencePanel as HTMLElement).getByText('Led modernization')).toBeInTheDocument();
    });

    it('keeps repeated identical roles renderable without duplicate key warnings', () => {
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
                            endDate: '2022-12-20',
                            descriptionMarkdown: 'Supported release work.',
                            skills: ['Testing'],
                            technologies: ['React']
                        }
                    ]
                })
            ],
            isLoading: false,
            error: null
        });

        render(<ResumePage />);

        expect(screen.getAllByText('Software Engineer')).toHaveLength(2);
        expect(consoleErrorSpy).not.toHaveBeenCalledWith(expect.stringContaining('Encountered two children with the same key'));
        consoleErrorSpy.mockRestore();
    });
});
