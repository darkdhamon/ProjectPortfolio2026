import type { JobRole } from '../../app/types';
import { formatWorkDateRange, renderWorkDescription } from './workHistorySupport';

interface JobRoleCardProps {
    jobRole: JobRole;
}

export function JobRoleCard({
    jobRole
}: JobRoleCardProps) {
    return (
        <article className="detail-panel work-role-card">
            <div className="work-role-heading">
                <div>
                    <p className="project-dates">{formatWorkDateRange(jobRole.startDate, jobRole.endDate)}</p>
                    <h3 className="work-role-title">{jobRole.role}</h3>
                </div>
                {jobRole.supervisorName?.trim() ? (
                    <p className="secondary-copy">Supervisor: {jobRole.supervisorName}</p>
                ) : null}
            </div>

            <div className="detail-markdown work-role-description">
                {renderWorkDescription(jobRole.descriptionMarkdown)}
            </div>

            {jobRole.skills.length > 0 ? (
                <div className="tag-group" aria-label={`${jobRole.role} skills`}>
                    {jobRole.skills.map(skill => (
                        <span key={skill} className="tag skill">{skill}</span>
                    ))}
                </div>
            ) : null}

            {jobRole.technologies.length > 0 ? (
                <div className="tag-group secondary" aria-label={`${jobRole.role} technologies`}>
                    {jobRole.technologies.map(technology => (
                        <span key={technology} className="tag technology">{technology}</span>
                    ))}
                </div>
            ) : null}
        </article>
    );
}
