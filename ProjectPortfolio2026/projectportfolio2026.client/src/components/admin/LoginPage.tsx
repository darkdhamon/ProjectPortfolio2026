import { useState, type SyntheticEvent } from 'react';
import { isAuthMockupPreviewEnabled } from './mockAuth';

export function LoginPage({
    redirectTo,
    onSignIn
}: {
    redirectTo: string;
    onSignIn: (redirectTo: string) => void;
}) {
    const [identifier, setIdentifier] = useState('');
    const [password, setPassword] = useState('');

    function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();
        onSignIn(redirectTo);
    }

    return (
        <main className="auth-page">
            <section className="auth-card">
                <div className="auth-intro">
                    <p className="eyebrow">Admin Access</p>
                    <h1>Sign in through the admin entry point.</h1>
                    <p className="hero-description">
                        This mockup keeps the interaction simple on purpose: the admin link is the only entry,
                        and a successful sign-in returns you to the protected route you originally asked for.
                    </p>
                </div>

                <form className="auth-form" onSubmit={handleSubmit}>
                    <label className="auth-field">
                        <span>Username or email</span>
                        <input
                            type="text"
                            name="identifier"
                            value={identifier}
                            onChange={event => setIdentifier(event.target.value)}
                            placeholder="admin or admin@example.com"
                        />
                    </label>

                    <label className="auth-field">
                        <span>Password</span>
                        <input
                            type="password"
                            name="password"
                            value={password}
                            onChange={event => setPassword(event.target.value)}
                            placeholder="Enter your password"
                        />
                    </label>

                    <div className="auth-actions">
                        <button className="primary-action" type="submit">
                            Log In
                        </button>
                        <p className="auth-helper">
                            Redirect target: <strong>{redirectTo}</strong>
                        </p>
                        {isAuthMockupPreviewEnabled ? (
                            <p className="auth-helper">
                                Mockup preview is enabled, so the admin screens can also be reviewed before sign-in wiring is enforced.
                            </p>
                        ) : null}
                    </div>
                </form>
            </section>
        </main>
    );
}
