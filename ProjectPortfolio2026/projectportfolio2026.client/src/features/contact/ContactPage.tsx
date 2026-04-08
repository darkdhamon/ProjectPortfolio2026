const contactChannels = [
    {
        label: 'Email',
        value: 'bronze@example.dev',
        href: 'mailto:bronze@example.dev',
        note: 'Best for interview requests, consulting inquiries, and longer-form conversations.'
    },
    {
        label: 'Phone',
        value: '(312) 555-0147',
        href: 'tel:+13125550147',
        note: 'Available for scheduled calls on weekdays between 9 AM and 5 PM Central.'
    },
    {
        label: 'Location',
        value: 'Chicago, Illinois',
        note: 'Open to remote roles, hybrid collaboration, and select on-site visits.'
    }
] as const;

const socialLinks = [
    {
        label: 'GitHub',
        href: 'https://github.com/darkdhamon',
        handle: '@darkdhamon',
        summary: 'Code samples, ongoing portfolio work, and implementation details.'
    },
    {
        label: 'LinkedIn',
        href: 'https://www.linkedin.com/in/bronze-loft',
        handle: 'Bronze Loft',
        summary: 'Professional background, role history, and recruiter-friendly context.'
    },
    {
        label: 'Calendly',
        href: 'https://calendly.com/bronze-loft/portfolio-intro',
        handle: 'Schedule an intro',
        summary: 'A lightweight path for a first conversation without email back-and-forth.'
    }
] as const;

const responsePrinciples = [
    'Clear communication on project scope, tradeoffs, and delivery progress.',
    'Comfort across frontend, backend, and architecture-heavy problem spaces.',
    'A preference for maintainable systems over one-off heroics.'
] as const;

export function ContactPage() {
    return (
        <main className="contact-page">
            <section className="contact-hero">
                <div className="contact-hero-copy">
                    <p className="eyebrow">Direct Contact</p>
                    <h1>Choose the contact path that fits the conversation you want to have.</h1>
                    <p className="hero-description">
                        This mockup frames outreach as a small, calm decision instead of a wall of links.
                        Recruiters can move quickly, collaborators can find the right channel, and optional
                        profile details can disappear cleanly when configuration is incomplete.
                    </p>
                </div>

                <div className="contact-availability-card" aria-label="Current availability">
                    <span className="stat-label">Current Availability</span>
                    <strong>Open to new opportunities</strong>
                    <p>
                        Focused on full-stack product engineering roles where API design, thoughtful UI,
                        and maintainable delivery all matter.
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
                        {contactChannels.map(channel => (
                            <section key={channel.label} className="contact-channel-card">
                                <span className="meta-label">{channel.label}</span>
                                {channel.href ? (
                                    <a href={channel.href}>{channel.value}</a>
                                ) : (
                                    <strong>{channel.value}</strong>
                                )}
                                <p>{channel.note}</p>
                            </section>
                        ))}
                    </div>
                </article>

                <article className="contact-panel contact-panel-emphasis">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">Social Links</p>
                        <h2>Profiles that reinforce the story.</h2>
                    </div>

                    <div className="social-link-list">
                        {socialLinks.map(link => (
                            <a
                                key={link.label}
                                className="social-link-card"
                                href={link.href}
                                target="_blank"
                                rel="noreferrer">
                                <div>
                                    <span className="meta-label">{link.label}</span>
                                    <strong>{link.handle}</strong>
                                </div>
                                <p>{link.summary}</p>
                            </a>
                        ))}
                    </div>
                </article>
            </section>

            <section className="contact-grid contact-grid-secondary">
                <article className="contact-panel">
                    <div className="contact-panel-heading">
                        <p className="eyebrow">What To Expect</p>
                        <h2>A portfolio contact page should answer more than “how.”</h2>
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
                        <p className="eyebrow">Mockup Notes</p>
                        <h2>Designed to become data-driven later.</h2>
                    </div>

                    <div className="contact-notes">
                        <p>
                            The card structure is intentionally friendly to future configuration work.
                            Contact rows, social links, and availability text can all come from a single
                            profile settings payload once the backend model exists.
                        </p>
                        <p>
                            For now, this gives issue #6 a concrete visual direction without forcing the
                            data model before the work history and resume issues are ready.
                        </p>
                    </div>
                </article>
            </section>
        </main>
    );
}
