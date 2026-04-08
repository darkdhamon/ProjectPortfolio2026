import { useState, type SyntheticEvent } from 'react';
import { type AccountDraft, type MockAuthUser, isAuthMockupPreviewEnabled } from './mockAuth';

export function AccountSettingsPage({
    currentUser,
    onSave
}: {
    currentUser: MockAuthUser;
    onSave: (draft: AccountDraft) => void;
}) {
    const [draft, setDraft] = useState<AccountDraft>({
        username: currentUser.username,
        email: currentUser.email,
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
        setPasswordDraft({
            currentPassword: '',
            newPassword: '',
            confirmPassword: ''
        });
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
                    {isAuthMockupPreviewEnabled ? (
                        <p className="secondary-copy">
                            This account page is also available in preview mode so we can review the layout before the live auth handshake is wired up.
                        </p>
                    ) : null}
                </div>

                <div className="admin-callout">
                    <span className="stat-label">Displayed as</span>
                    <strong>{resolvedDisplayName}</strong>
                    <p className="secondary-copy">Role: {currentUser.roles.join(', ')}</p>
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

                    <button className="primary-action" type="submit">Save Profile Mockup</button>
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

                    <button className="primary-action secondary-action" type="submit">Save Password Mockup</button>
                </form>
            </section>
        </main>
    );
}
