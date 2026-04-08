interface WorkHistoryHeaderProps {
    employerCount: number;
    roleCount: number;
}

export function WorkHistoryHeader({
    employerCount,
    roleCount
}: WorkHistoryHeaderProps) {
    return (
        <section className="hero-panel work-history-hero">
            <p className="eyebrow">Work History</p>
            <div className="hero-copy">
                <div>
                    <h1>Track employers, roles, and the skill context behind each chapter of the resume timeline.</h1>
                    <p className="hero-description">
                        This view is driven by the same structured role data that will later support resume generation,
                        alternate address display modes, and richer admin editing.
                    </p>
                </div>

                <div className="hero-stats" aria-label="Work history summary">
                    <div className="stat-card">
                        <span className="stat-label">Employers</span>
                        <strong>{employerCount}</strong>
                    </div>
                    <div className="stat-card">
                        <span className="stat-label">Job Roles</span>
                        <strong>{roleCount}</strong>
                    </div>
                    <div className="stat-card">
                        <span className="stat-label">Address Mode</span>
                        <strong>City / Region</strong>
                    </div>
                </div>
            </div>
        </section>
    );
}
