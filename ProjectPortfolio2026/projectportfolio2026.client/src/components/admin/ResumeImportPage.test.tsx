import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ResumeImportPage } from './ResumeImportPage';

afterEach(() => {
    cleanup();
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

        expect(onParseUpload).toHaveBeenCalledWith(file);
        expect(await screen.findByRole('heading', { name: 'Candidate data is ready for the next import stages' })).toBeInTheDocument();
        expect(screen.getByText('resume.pdf')).toBeInTheDocument();
        expect(screen.getByText('1 role candidate')).toBeInTheDocument();
        expect(screen.getByText('1 employer group and 2 global skills')).toBeInTheDocument();
        expect(screen.getAllByText('Portfolio Owner').length).toBeGreaterThan(0);
    });
});
