import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResumeConfigurationSection } from './ResumeConfigurationSection';

vi.mock('../../api/resumeConfiguration', () => ({
    fetchAdminResumeConfiguration: vi.fn(),
    saveResumeConfigurationAsync: vi.fn()
}));

import {
    fetchAdminResumeConfiguration,
    saveResumeConfigurationAsync
} from '../../api/resumeConfiguration';

const mockFetchAdminResumeConfiguration = vi.mocked(fetchAdminResumeConfiguration);
const mockSaveResumeConfigurationAsync = vi.mocked(saveResumeConfigurationAsync);

describe('ResumeConfigurationSection', () => {
    beforeEach(() => {
        mockFetchAdminResumeConfiguration.mockResolvedValue({
            id: 1,
            sourceType: 'hosted-file',
            sourceUrl: 'https://cdn.example.dev/resume.pdf',
            displayLabel: 'Download Resume',
            summary: 'ATS-friendly PDF.',
            isConfigured: true
        });
    });

    afterEach(() => {
        cleanup();
        vi.clearAllMocks();
    });

    it('loads the saved admin configuration into the form', async () => {
        render(<ResumeConfigurationSection />);

        expect(await screen.findByDisplayValue('https://cdn.example.dev/resume.pdf')).toBeInTheDocument();
        expect(screen.getByDisplayValue('Download Resume')).toBeInTheDocument();
        expect(screen.getByDisplayValue('ATS-friendly PDF.')).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Visible from the public resume page' })).toBeInTheDocument();
    });

    it('validates required fields before saving an enabled source', async () => {
        mockFetchAdminResumeConfiguration.mockResolvedValue({
            id: 0,
            sourceType: 'none',
            sourceUrl: null,
            displayLabel: null,
            summary: null,
            isConfigured: false
        });

        render(<ResumeConfigurationSection />);

        await screen.findByRole('button', { name: 'Save Resume Configuration' });
        fireEvent.change(screen.getByLabelText('Source type'), {
            target: { value: 'embed' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Save Resume Configuration' }));

        expect(await screen.findByText('A resume source URL is required when a hosted or embedded source is enabled.')).toBeInTheDocument();
        expect(mockSaveResumeConfigurationAsync).not.toHaveBeenCalled();
    });

    it('saves the edited configuration and surfaces a success notice', async () => {
        mockSaveResumeConfigurationAsync.mockResolvedValue({
            id: 1,
            sourceType: 'embed',
            sourceUrl: 'https://drive.example.dev/embed/resume',
            displayLabel: 'Open Embedded Resume',
            summary: 'External embed-ready source.',
            isConfigured: true
        });

        render(<ResumeConfigurationSection />);

        await screen.findByDisplayValue('https://cdn.example.dev/resume.pdf');

        fireEvent.change(screen.getByLabelText('Source type'), {
            target: { value: 'embed' }
        });
        fireEvent.change(screen.getByLabelText('Source URL'), {
            target: { value: 'https://drive.example.dev/embed/resume' }
        });
        fireEvent.change(screen.getByLabelText('Public label'), {
            target: { value: 'Open Embedded Resume' }
        });
        fireEvent.change(screen.getByLabelText('Public summary'), {
            target: { value: 'External embed-ready source.' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Save Resume Configuration' }));

        await waitFor(() => {
            expect(mockSaveResumeConfigurationAsync).toHaveBeenCalledWith({
                sourceType: 'embed',
                sourceUrl: 'https://drive.example.dev/embed/resume',
                displayLabel: 'Open Embedded Resume',
                summary: 'External embed-ready source.'
            });
        });

        expect(await screen.findByText('Resume configuration saved.')).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Visible from the public resume page' })).toBeInTheDocument();
    });
});
