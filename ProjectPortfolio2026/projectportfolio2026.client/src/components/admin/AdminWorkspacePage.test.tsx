import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AdminWorkspacePage } from './AdminWorkspacePage';

afterEach(() => {
    cleanup();
});

describe('AdminWorkspacePage', () => {
    it('renders the overview cards and account shortcut', () => {
        render(
            <AdminWorkspacePage
                activeSection="dashboard"
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
            />
        );

        expect(screen.getByRole('heading', { name: 'Content Management Overview' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Open Account Settings' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Open Project Management' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Open Social Links' })).toBeInTheDocument();
    });

    it('wires the overview action links to the expected admin routes', () => {
        const onNavigate = vi.fn();

        render(
            <AdminWorkspacePage
                activeSection="dashboard"
                currentUserDisplayName="Portfolio Owner"
                onNavigate={onNavigate}
            />
        );

        const accountSettingsLink = screen.getByRole('link', { name: 'Open Account Settings' });
        const projectManagementLink = screen.getByRole('link', { name: 'Open Project Management' });
        const socialLinksLink = screen.getByRole('link', { name: 'Open Social Links' });
        const resumeConfigurationLink = screen.getByRole('link', { name: 'Open Resume Configuration' });
        const publishingLink = screen.getByRole('link', { name: 'Open Publishing And Preview' });

        expect(accountSettingsLink).toHaveAttribute('href', '/admin/account');
        expect(projectManagementLink).toHaveAttribute('href', '/admin/projects');
        expect(socialLinksLink).toHaveAttribute('href', '/admin/social-links');
        expect(resumeConfigurationLink).toHaveAttribute('href', '/admin/resume');
        expect(publishingLink).toHaveAttribute('href', '/admin/publishing');

        fireEvent.click(accountSettingsLink);
        fireEvent.click(projectManagementLink);
        fireEvent.click(socialLinksLink);
        fireEvent.click(resumeConfigurationLink);
        fireEvent.click(publishingLink);

        expect(onNavigate).toHaveBeenNthCalledWith(1, '/admin/account', { preserveScroll: true });
        expect(onNavigate).toHaveBeenNthCalledWith(2, '/admin/projects', { preserveScroll: true });
        expect(onNavigate).toHaveBeenNthCalledWith(3, '/admin/social-links', { preserveScroll: true });
        expect(onNavigate).toHaveBeenNthCalledWith(4, '/admin/resume', { preserveScroll: true });
        expect(onNavigate).toHaveBeenNthCalledWith(5, '/admin/publishing', { preserveScroll: true });
    });

    it('highlights the active section and shows its placeholder guidance', () => {
        render(
            <AdminWorkspacePage
                activeSection="resume"
                currentUserDisplayName="Portfolio Owner"
                onNavigate={vi.fn()}
            />
        );

        expect(screen.getByRole('heading', { name: 'Resume Configuration' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Resume configuration can build on a stable shell.' })).toBeInTheDocument();
        expect(screen.getByText('Tracked issues: #67, #61')).toBeInTheDocument();

        const navigation = screen.getByRole('navigation', { name: 'Admin workspace sections' });
        expect(within(navigation).getByRole('link', { name: /Resume Configuration/ })).toHaveAttribute('aria-current', 'page');
    });

    it('returns section detail pages back to the admin overview', () => {
        const onNavigate = vi.fn();

        render(
            <AdminWorkspacePage
                activeSection="resume"
                currentUserDisplayName="Portfolio Owner"
                onNavigate={onNavigate}
            />
        );

        const returnLink = screen.getByRole('link', { name: 'Return to overview' });

        expect(returnLink).toHaveAttribute('href', '/admin');

        fireEvent.click(returnLink);

        expect(onNavigate).toHaveBeenCalledWith('/admin', { preserveScroll: true });
    });

    it('links the resume section into the resume import start flow', () => {
        const onNavigate = vi.fn();

        render(
            <AdminWorkspacePage
                activeSection="resume"
                currentUserDisplayName="Portfolio Owner"
                onNavigate={onNavigate}
            />
        );

        const importLink = screen.getByRole('link', { name: 'Start Resume Import' });

        expect(importLink).toHaveAttribute('href', '/admin/resume/import');

        fireEvent.click(importLink);

        expect(onNavigate).toHaveBeenCalledWith('/admin/resume/import', { preserveScroll: true });
    });
});
