import { useState, type SyntheticEvent } from 'react';
import { type AccountDraft, type AuthUser } from './mockAuth';

export function AccountSettingsPage({
    currentUser,
    isSavingProfile,
    profileError,
    profileNotice,
    isSavingPassword,
    passwordError,
    passwordNotice,
    onSave,
    onChangePassword
}: {
    currentUser: AuthUser;
    isSavingProfile: boolean;
    profileError: string | null;
    profileNotice: string | null;
    isSavingPassword: boolean;
    passwordError: string | null;
    passwordNotice: string | null;
    onSave: (draft: AccountDraft) => void;
    onChangePassword: (currentPassword: string, newPassword: string, confirmPassword: string) => void;
}) {
    const [draft, setDraft] = useState<AccountDraft>({
        username: currentUser.username,
        email: currentUser.email ?? '',
        displayName: currentUser.displayName
    });
    const [passwordDraft, setPasswordDraft] = useState({
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
    });
    const resolvedDisplayName = currentUser.displayName.trim() || currentUser.username;

    function handleProfileSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();
        onSave(draft);
    }

    function handlePasswordSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();
        onChangePassword(passwordDraft.currentPassword, passwordDraft.newPassword, passwordDraft.confirmPassword);
    }

    return (
        <main className="admin-page">
            <section className="admin-hero account-hero">
                <div>
                    <p className="eyebrow">Account Profile</p>
                    <h1>Manage your current admin account</h1>
                    <p className="hero-description">
                        Display name is optional here, so the mockup shows the username as the fallback label when no separate display name is set.
                    </p>
                </div>

                <div className="admin-callout">
                    <span className="stat-label">Displayed as</span>
                    <strong>{resolvedDisplayName}</strong>
                    <p className="secondary-copy">Role: {currentUser.isAdmin ? 'Admin' : 'User'}</p>
                </div>
            </section>

            <section className="account-grid">
                <form className="admin-card account-form" onSubmit={handleProfileSubmit}>
                    <p className="eyebrow">Profile</p>
                    <h2>Editable identity fields</h2>

                    <label className="auth-field">
                        <span>Username</span>
                        <input
                            type="text"
                            value={draft.username}
                            onChange={event => setDraft(current => ({ ...current, username: event.target.value }))}
                        />
                    </label>

                    <label className="auth-field">
                        <span>Email</span>
                        <input
                            type="email"
                            value={draft.email}
                            onChange={event => setDraft(current => ({ ...current, email: event.target.value }))}
                        />
                    </label>

                    <label className="auth-field">
                        <span>Display name</span>
                        <input
                            type="text"
                            value={draft.displayName}
                            onChange={event => setDraft(current => ({ ...current, displayName: event.target.value }))}
                            placeholder="Optional friendly display name"
                        />
                    </label>

                    {profileError ? <p className="status-banner error">{profileError}</p> : null}
                    {profileNotice ? <p className="status-banner">{profileNotice}</p> : null}

                    <button className="primary-action" type="submit" disabled={isSavingProfile}>
                        {isSavingProfile ? 'Saving Profile...' : 'Save Profile'}
                    </button>
                </form>

                <form className="admin-card account-form" onSubmit={handlePasswordSubmit}>
                    <p className="eyebrow">Password</p>
                    <h2>Password change flow</h2>

                    <label className="auth-field">
                        <span>Current password</span>
                        <input
                            type="password"
                            value={passwordDraft.currentPassword}
                            onChange={event => setPasswordDraft(current => ({ ...current, currentPassword: event.target.value }))}
                        />
                    </label>

                    <label className="auth-field">
                        <span>New password</span>
                        <input
                            type="password"
                            value={passwordDraft.newPassword}
                            onChange={event => setPasswordDraft(current => ({ ...current, newPassword: event.target.value }))}
                        />
                    </label>

                    <label className="auth-field">
                        <span>Confirm new password</span>
                        <input
                            type="password"
                            value={passwordDraft.confirmPassword}
                            onChange={event => setPasswordDraft(current => ({ ...current, confirmPassword: event.target.value }))}
                        />
                    </label>

                    {passwordError ? <p className="status-banner error">{passwordError}</p> : null}
                    {passwordNotice ? <p className="status-banner">{passwordNotice}</p> : null}

                    <button className="primary-action secondary-action" type="submit" disabled={isSavingPassword}>
                        {isSavingPassword ? 'Saving Password...' : 'Save Password'}
                    </button>
                </form>
            </section>
        </main>
    );
}
