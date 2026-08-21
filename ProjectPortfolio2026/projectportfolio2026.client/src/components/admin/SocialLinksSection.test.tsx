import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SocialLinksSection } from './SocialLinksSection';

vi.mock('../../api/socialLinks', () => ({
    fetchAdminSocialLinks: vi.fn(),
    saveAdminSocialLinks: vi.fn()
}));

import {
    fetchAdminSocialLinks,
    saveAdminSocialLinks
} from '../../api/socialLinks';

const mockFetchAdminSocialLinks = vi.mocked(fetchAdminSocialLinks);
const mockSaveAdminSocialLinks = vi.mocked(saveAdminSocialLinks);

describe('SocialLinksSection', () => {
    beforeEach(() => {
        mockFetchAdminSocialLinks.mockResolvedValue([
            {
                id: 1,
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/darkdhamon',
                handle: '@darkdhamon',
                summary: 'Primary profile',
                sortOrder: 1,
                isVisible: true
            }
        ]);
        mockSaveAdminSocialLinks.mockResolvedValue([
            {
                id: 1,
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/darkdhamon',
                handle: '@darkdhamon',
                summary: 'Primary profile',
                sortOrder: 1,
                isVisible: true
            }
        ]);
    });

    afterEach(() => {
        cleanup();
        vi.clearAllMocks();
    });

    it('loads social links for editing', async () => {
        render(<SocialLinksSection />);

        expect(await screen.findByLabelText('Platform 1')).toHaveValue('github');
        expect(screen.getByLabelText('Label 1')).toHaveValue('GitHub');
        expect(screen.getByLabelText('URL 1')).toHaveValue('https://github.com/darkdhamon');
    });

    it('disables draft mutations while social links are loading', () => {
        mockFetchAdminSocialLinks.mockReturnValue(new Promise(() => undefined));

        render(<SocialLinksSection />);

        expect(screen.getByRole('button', { name: 'Add social link' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Clear draft' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Save social links' })).toBeDisabled();
    });

    it('keeps destructive actions disabled after loading fails', async () => {
        mockFetchAdminSocialLinks.mockRejectedValue(new Error('Unable to load social links.'));

        render(<SocialLinksSection />);

        expect(await screen.findByText('Unable to load social links.')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Add social link' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Clear draft' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Save social links' })).toBeDisabled();
    });

    it('adds and removes social links in local draft state', async () => {
        render(<SocialLinksSection />);

        await screen.findByLabelText('Platform 1');

        fireEvent.click(screen.getByRole('button', { name: 'Add social link' }));
        expect(screen.getByLabelText('Platform 2')).toBeInTheDocument();

        const clearButton = screen.getByRole('button', { name: 'Clear draft' });
        fireEvent.click(clearButton);

        expect(screen.queryByLabelText('Platform 1')).not.toBeInTheDocument();
    });

    it('validates required fields before saving', async () => {
        render(<SocialLinksSection />);

        await screen.findByLabelText('Platform 1');
        fireEvent.change(screen.getByLabelText('Platform 1'), { target: { value: '' } });
        fireEvent.click(screen.getByRole('button', { name: 'Save social links' }));

        expect(await screen.findByText('Fix the highlighted validation issues before saving.')).toBeInTheDocument();
        expect(mockSaveAdminSocialLinks).not.toHaveBeenCalled();
    });

    it('saves social links and shows a success notice', async () => {
        render(<SocialLinksSection />);

        await screen.findByLabelText('Platform 1');
        fireEvent.change(screen.getByLabelText('URL 1'), { target: { value: 'https://github.com/new-handle' } });
        fireEvent.click(screen.getByRole('button', { name: 'Save social links' }));

        await waitFor(() => {
            expect(mockSaveAdminSocialLinks).toHaveBeenCalledWith([{
                id: 1,
                platform: 'github',
                label: 'GitHub',
                url: 'https://github.com/new-handle',
                handle: '@darkdhamon',
                summary: 'Primary profile',
                sortOrder: 1,
                isVisible: true
            }]);
            expect(screen.getByText('Social links saved.')).toBeInTheDocument();
        });
    });
});
