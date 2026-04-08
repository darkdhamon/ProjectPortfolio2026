import { usePortfolioProfile } from '../../hooks/usePortfolioProfile';

const responsePrinciples = [
    'Clear communication on project scope, tradeoffs, and delivery progress.',
    'Comfort across frontend, backend, and architecture-heavy problem spaces.',
    'A preference for maintainable systems over one-off heroics.'
] as const;

export function ContactPage() {
    const { profile, isLoading, error, isMissing } = usePortfolioProfile();
    const contactMethods = profile?.contactMethods ?? [];
    const socialLinks = profile?.socialLinks ?? [];

    return (
        <main className="contact-page">
            {error ? <p className="status-banner error">{error}</p> : null}
            {!error && isLoading ? <p className="status-banner">Loading contact profile...</p> : null}
            {!error && !isLoading && isMissing ? (
                <p className="status-banner">The public contact profile has not been configured yet.</p>
            ) : null}

            <section className="contact-hero">
                <div className="contact-hero-copy">
                    <p className="eyebrow">Direct Contact</p>
                    <h1>{profile?.contactHeadline ?? 'Choose the contact path that fits the conversation you want to have.'}</h1>
                    <p className="hero-description">
                        {profile?.contactIntro ?? 'Contact details will appear here once the portfolio profile is available.'}
                    </p>
                </div>

                <div className="contact-availability-card" aria-label="Current availability">
                    <span className="stat-label">Current Availability</span>
                    <strong>{profile?.availabilityHeadline ?? 'Availability not configured'}</strong>
                    <p>
                        {profile?.availabilitySummary ?? 'Add a portfolio profile to publish an availability summary for the contact page.'}
                    </p>
                </div>
            </section>

            <section className="contact-grid">
                <article className="contact-panel">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">Primary Channels</p>
                        <h2>Reach out without guesswork.</h2>
                    </div>

                    <div className="contact-channel-list">
                        {contactMethods.length > 0 ? contactMethods.map(contactMethod => (
                            <section
                                key={`${contactMethod.type}-${contactMethod.label}-${contactMethod.sortOrder}`}
                                className="contact-channel-card">
                                <span className="meta-label">{contactMethod.label}</span>
                                {contactMethod.href ? (
                                    <a href={contactMethod.href}>{contactMethod.value}</a>
                                ) : (
                                    <strong>{contactMethod.value}</strong>
                                )}
                                {contactMethod.note ? <p>{contactMethod.note}</p> : null}
                            </section>
                        )) : (
                            <p className="secondary-copy">Public contact methods will appear here once they are configured.</p>
                        )}
                    </div>
                </article>

                <article className="contact-panel contact-panel-emphasis">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">Social Links</p>
                        <h2>Profiles that reinforce the story.</h2>
                    </div>

                    <div className="social-link-list">
                        {socialLinks.length > 0 ? socialLinks.map(link => (
                            <a
                                key={`${link.platform}-${link.label}-${link.sortOrder}`}
                                className="social-link-card"
                                href={link.url}
                                target="_blank"
                                rel="noreferrer">
                                <div>
                                    <span className="meta-label">{link.label}</span>
                                    <strong>{link.handle ?? link.label}</strong>
                                </div>
                                {link.summary ? <p>{link.summary}</p> : null}
                            </a>
                        )) : (
                            <p className="secondary-copy">Public social links will appear here once they are configured.</p>
                        )}
                    </div>
                </article>
            </section>

            <section className="contact-grid contact-grid-secondary">
                <article className="contact-panel">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">What To Expect</p>
                        <h2>A portfolio contact page should answer more than "how."</h2>
                    </div>

                    <div className="contact-principles">
                        {responsePrinciples.map(principle => (
                            <div key={principle} className="principle-card">
                                <span className="principle-bullet" aria-hidden="true" />
                                <p>{principle}</p>
                            </div>
                        ))}
                    </div>
                </article>

                <article className="contact-panel">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">Profile Model</p>
                        <h2>Built around a portfolio profile aggregate.</h2>
                    </div>

                    <div className="contact-notes">
                        <p>
                            Contact methods and social links are now loaded from the server through a
                            portfolio profile entity instead of being hardcoded in the client.
                        </p>
                        <p>
                            Visibility is handled in the data model so the future admin experience can
                            show or hide individual contact methods and platforms without changing the page layout.
                        </p>
                    </div>
                </article>
            </section>
        </main>
    );
}
