import { useWorkHistory } from '../../hooks/useWorkHistory';
import { EmployerCard } from './EmployerCard';
import { WorkHistoryHeader } from './WorkHistoryHeader';

export function WorkHistoryPage() {
    const {
        employers,
        isLoading,
        error
    } = useWorkHistory();
    const roleCount = employers.reduce((total, employer) => total + employer.jobRoles.length, 0);

    return (
        <main className="portfolio-page work-history-page">
            <WorkHistoryHeader
                employerCount={employers.length}
                roleCount={roleCount}
            />

            {isLoading ? <p className="status-banner">Loading work history...</p> : null}
            {error ? <p className="status-banner error">{error}</p> : null}
            {!isLoading && !error && employers.length === 0 ? (
                <p className="status-banner">Work history will appear here once published employer records are available.</p>
            ) : null}

            <div className="work-history-stack">
                {employers.map(employer => (
                    <EmployerCard
                        key={employer.id}
                        employer={employer}
                    />
                ))}
            </div>
        </main>
    );
}
