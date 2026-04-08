import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { AccountSettingsPage } from './AccountSettingsPage';

describe('AccountSettingsPage', () => {
    it('falls back to the username, renders notices, and submits both forms', () => {
        const onSave = vi.fn();
        const onChangePassword = vi.fn();

        render(
            <AccountSettingsPage
                currentUser={{
                    username: 'admin-user',
                    email: 'admin@example.com',
                    displayName: '   ',
                    isAdmin: true
                }}
                isSavingProfile={false}
                profileError="Profile error"
                profileNotice="Profile saved."
                isSavingPassword={false}
                passwordError="Password error"
                passwordNotice="Password updated."
                onSave={onSave}
                onChangePassword={onChangePassword}
            />
        );

        expect(screen.getByText('admin-user')).toBeInTheDocument();
        expect(screen.getByText('Role: Admin')).toBeInTheDocument();
        expect(screen.getByText('Profile error')).toBeInTheDocument();
        expect(screen.getByText('Profile saved.')).toBeInTheDocument();
        expect(screen.getByText('Password error')).toBeInTheDocument();
        expect(screen.getByText('Password updated.')).toBeInTheDocument();

        fireEvent.change(screen.getByLabelText('Username'), {
            target: { value: 'portfolio-admin' }
        });
        fireEvent.change(screen.getByLabelText('Email'), {
            target: { value: 'portfolio@example.com' }
        });
        fireEvent.change(screen.getByLabelText('Display name'), {
            target: { value: 'Portfolio Owner' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));

        expect(onSave).toHaveBeenCalledWith({
            username: 'portfolio-admin',
            email: 'portfolio@example.com',
            displayName: 'Portfolio Owner'
        });

        fireEvent.change(screen.getByLabelText('Current password'), {
            target: { value: 'Password@1' }
        });
        fireEvent.change(screen.getByLabelText('New password'), {
            target: { value: 'Password@2' }
        });
        fireEvent.change(screen.getByLabelText('Confirm new password'), {
            target: { value: 'Password@2' }
        });
        fireEvent.click(screen.getByRole('button', { name: 'Save Password' }));

        expect(onChangePassword).toHaveBeenCalledWith('Password@1', 'Password@2', 'Password@2');
    });

    it('shows saving labels and the non-admin role label', () => {
        render(
            <AccountSettingsPage
                currentUser={{
                    username: 'editor',
                    email: '',
                    displayName: 'Editor',
                    isAdmin: false
                }}
                isSavingProfile={true}
                profileError={null}
                profileNotice={null}
                isSavingPassword={true}
                passwordError={null}
                passwordNotice={null}
                onSave={vi.fn()}
                onChangePassword={vi.fn()}
            />
        );

        expect(screen.getByText('Role: User')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Saving Profile...' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Saving Password...' })).toBeDisabled();
    });
});
