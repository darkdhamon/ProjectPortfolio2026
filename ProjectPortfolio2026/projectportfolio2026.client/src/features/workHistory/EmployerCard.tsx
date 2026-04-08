import type { Employer } from '../../app/types';
import { JobRoleCard } from './JobRoleCard';
import { formatEmployerLocation } from './workHistorySupport';

interface EmployerCardProps {
    employer: Employer;
}

export function EmployerCard({
    employer
}: EmployerCardProps) {
    return (
        <section className="work-history-employer">
            <header className="detail-panel work-employer-header">
                <div>
                    <p className="eyebrow">Employer</p>
                    <h2>{employer.name}</h2>
                </div>
                <p className="secondary-copy">{formatEmployerLocation(employer.city, employer.region)}</p>
            </header>

            <div className="work-role-list">
                {employer.jobRoles.map(jobRole => (
                    <JobRoleCard
                        key={`${employer.id}-${jobRole.role}-${jobRole.startDate}`}
                        jobRole={jobRole}
                    />
                ))}
            </div>
        </section>
    );
}
