import { useState, type SyntheticEvent } from 'react';

export function LoginPage({
    redirectTo,
    isSubmitting,
    errorMessage,
    onSignIn
}: {
    redirectTo: string;
    isSubmitting: boolean;
    errorMessage: string | null;
    onSignIn: (login: string, password: string, redirectTo: string) => void;
}) {
    const [identifier, setIdentifier] = useState('');
    const [password, setPassword] = useState('');

    function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();
        onSignIn(identifier, password, redirectTo);
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
                        <button className="primary-action" type="submit" disabled={isSubmitting}>
                            {isSubmitting ? 'Signing In...' : 'Log In'}
                        </button>
                        <p className="auth-helper">
                            Redirect target: <strong>{redirectTo}</strong>
                        </p>
                        {errorMessage ? <p className="status-banner error">{errorMessage}</p> : null}
                    </div>
                </form>
            </section>
        </main>
    );
}
