import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ResumeImportPage } from './ResumeImportPage';

afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
});

describe('ResumeImportPage', () => {
    it('lets admins choose the configured source path and switch back to upload', () => {
        const onParseConfigured = vi.fn().mockResolvedValue({
            globalSkills: [],
            candidateWorkHistory: []
        });

        render(
            <ResumeImportPage
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
                onParseUpload={vi.fn()}
                onParseConfigured={onParseConfigured}
            />
        );

        fireEvent.click(screen.getByRole('button', { name: 'Select configured source' }));

        expect(screen.getByRole('heading', { name: 'Parse from the configured master resume source' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Parse Configured Resume Source' })).toBeInTheDocument();
        expect(screen.getByText('The configured source is fetched only when this workflow is explicitly started, then routed through the same import parser used by upload.')).toBeInTheDocument();

        fireEvent.submit(screen.getByRole('button', { name: 'Parse Configured Resume Source' }).closest('form')!);
        expect(onParseConfigured).toHaveBeenCalledTimes(1);
    });

    it('starts the upload import flow and renders a parse summary', async () => {
        const onParseUpload = vi.fn().mockResolvedValue({
            sourceFileName: 'resume.pdf',
            parserName: 'StubParser',
            globalSkills: ['C#', '.NET'],
            candidateWorkHistory: [
                {
                    candidateId: 'employer-001',
                    employerName: 'Northwind Health',
                    jobRoles: [
                        {
                            candidateId: 'role-001',
                            jobTitle: 'Senior Software Engineer',
                            descriptionLines: [],
                            skills: [],
                            technologies: []
                        }
                    ]
                }
            ],
            person: {
                fullName: 'Portfolio Owner',
                headline: 'Full-stack engineer',
                emailAddress: 'owner@example.com',
                phoneNumbers: [],
                socialProfiles: []
            }
        });

        render(
            <ResumeImportPage
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
                onParseUpload={onParseUpload}
            />
        );

        fireEvent.click(screen.getByRole('button', { name: 'Select upload source' }));

        const file = new File(['resume'], 'resume.pdf', { type: 'application/pdf' });
        fireEvent.change(screen.getByLabelText('Resume file'), {
            target: {
                files: [file]
            }
        });
        fireEvent.submit(screen.getByRole('button', { name: 'Parse Uploaded Resume' }).closest('form')!);

        expect(onParseUpload).toHaveBeenCalledWith(file, expect.any(AbortSignal));
        expect(await screen.findByRole('heading', { name: 'Candidate data is ready for the next import stages' })).toBeInTheDocument();
        expect(screen.getByText('resume.pdf')).toBeInTheDocument();
        expect(screen.getByText('1 role candidate')).toBeInTheDocument();
        expect(screen.getByText('1 employer group and 2 global skills')).toBeInTheDocument();
        expect(screen.getAllByText('Portfolio Owner').length).toBeGreaterThan(0);
    });

    it('keeps the current source active when the admin declines to cancel an in-flight parse', () => {
        const onParseUpload = vi.fn((_: File, signal?: AbortSignal) => new Promise<never>((_, reject) => {
            signal?.addEventListener(
                'abort',
                () => reject(new DOMException('The request was aborted.', 'AbortError')),
                { once: true }
            );
        }));
        const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

        render(
            <ResumeImportPage
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
                onParseUpload={onParseUpload}
            />
        );

        fireEvent.click(screen.getByRole('button', { name: 'Select upload source' }));

        const file = new File(['resume'], 'resume.pdf', { type: 'application/pdf' });
        fireEvent.change(screen.getByLabelText('Resume file'), {
            target: {
                files: [file]
            }
        });
        fireEvent.submit(screen.getByRole('button', { name: 'Parse Uploaded Resume' }).closest('form')!);
        fireEvent.click(screen.getByRole('button', { name: 'Select configured source' }));

        expect(confirmSpy).toHaveBeenCalledWith('A resume parse is still in progress. Cancel the current parse before switching sources?');
        expect(screen.getByRole('heading', { name: 'Upload and stage a resume for parsing' })).toBeInTheDocument();
        expect(screen.queryByRole('heading', { name: 'Parse from the configured master resume source' })).not.toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Parsing Resume...' })).toBeDisabled();
    });

    it('cancels the in-flight parse before switching sources when the admin confirms', async () => {
        let capturedSignal: AbortSignal | undefined;
        const onParseUpload = vi.fn((_: File, signal?: AbortSignal) => new Promise<never>((_, reject) => {
            capturedSignal = signal;
            signal?.addEventListener(
                'abort',
                () => reject(new DOMException('The request was aborted.', 'AbortError')),
                { once: true }
            );
        }));
        const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

        render(
            <ResumeImportPage
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
                onParseUpload={onParseUpload}
            />
        );

        fireEvent.click(screen.getByRole('button', { name: 'Select upload source' }));

        const file = new File(['resume'], 'resume.pdf', { type: 'application/pdf' });
        fireEvent.change(screen.getByLabelText('Resume file'), {
            target: {
                files: [file]
            }
        });
        fireEvent.submit(screen.getByRole('button', { name: 'Parse Uploaded Resume' }).closest('form')!);
        fireEvent.click(screen.getByRole('button', { name: 'Select configured source' }));

        expect(confirmSpy).toHaveBeenCalledWith('A resume parse is still in progress. Cancel the current parse before switching sources?');

        await waitFor(() => {
            expect(capturedSignal?.aborted).toBe(true);
        });

        expect(screen.getByRole('heading', { name: 'Parse from the configured master resume source' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Parse Configured Resume Source' })).toBeEnabled();
        expect(screen.queryByText('Unable to start the resume import.')).not.toBeInTheDocument();
    });
});
