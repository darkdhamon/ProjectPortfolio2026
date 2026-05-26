import { cleanup, render, screen, within } from '@testing-library/react';
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
});
