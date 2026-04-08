import { InternalLink, type NavigateHandler } from '../navigation/InternalLink';
import { isAuthMockupPreviewEnabled } from './mockAuth';

export function AdminDashboardPage({
    currentUserDisplayName,
    onNavigate
}: {
    currentUserDisplayName: string;
    onNavigate: NavigateHandler;
}) {
    return (
        <main className="admin-page">
            <section className="admin-hero">
                <div>
                    <p className="eyebrow">Protected Area</p>
                    <h1>Admin dashboard mockup</h1>
                    <p className="hero-description">
                        This placeholder confirms the protected route, the nav behavior, and the general tone of the admin area before live API wiring begins.
                    </p>
                    {isAuthMockupPreviewEnabled ? (
                        <p className="secondary-copy">
                            Mockup preview mode is active right now, so this screen stays visible even before authentication is fully enforced.
                        </p>
                    ) : null}
                </div>

                <div className="admin-callout">
                    <span className="stat-label">Current admin</span>
                    <strong>{currentUserDisplayName}</strong>
                    <p className="secondary-copy">The live version will replace this local mock state with the authenticated session endpoint.</p>
                </div>
            </section>

            <section className="admin-grid">
                <article className="admin-card">
                    <p className="eyebrow">Stage 1</p>
                    <h2>Authentication flow</h2>
                    <p>The sidebar, login screen, and protected route flow are now represented in the UI layer.</p>
                </article>

                <article className="admin-card">
                    <p className="eyebrow">Stage 2</p>
                    <h2>Account settings</h2>
                    <p>Account editing is mocked so we can shape the field layout and interaction before endpoint hookup.</p>
                    <InternalLink
                        className="primary-link"
                        href="/admin/account"
                        onNavigate={onNavigate}>
                        Open Account Settings
                    </InternalLink>
                </article>

                <article className="admin-card">
                    <p className="eyebrow">Stage 3</p>
                    <h2>Future admin tools</h2>
                    <p>Project management, publishing controls, and first-run setup integration can land here once the API grows.</p>
                </article>
            </section>
        </main>
    );
}
