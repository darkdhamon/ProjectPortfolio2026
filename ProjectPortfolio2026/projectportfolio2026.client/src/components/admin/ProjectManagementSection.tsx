import { useCallback, useEffect, useMemo, useState } from 'react';
import {
    fetchAdminProjects,
    setFeaturedProjectOrder,
    setProjectFeaturedState
} from '../../api/adminProjects';
import type { ProjectSummary } from '../../app/types';

function normalizeStartDate(value: string) {
    return new Date(`${value}T00:00:00`);
}

function sortByStartDate(projects: ProjectSummary[]) {
    return [...projects].sort((left, right) => {
        const startDateCompare = normalizeStartDate(right.startDate).getTime() - normalizeStartDate(left.startDate).getTime();
        if (startDateCompare !== 0) {
            return startDateCompare;
        }

        return left.title.localeCompare(right.title, undefined, { sensitivity: 'base' });
    });
}

function sortFeaturedProjects(projects: ProjectSummary[]) {
    return [...projects].sort((left, right) => {
        const leftOrder = left.featuredOrder ?? Number.MAX_SAFE_INTEGER;
        const rightOrder = right.featuredOrder ?? Number.MAX_SAFE_INTEGER;

        if (leftOrder !== rightOrder) {
            return leftOrder - rightOrder;
        }

        return normalizeStartDate(right.startDate).getTime() - normalizeStartDate(left.startDate).getTime();
    });
}

export function ProjectManagementSection() {
    const [projects, setProjects] = useState<ProjectSummary[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [notice, setNotice] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [inFlightProjectId, setInFlightProjectId] = useState<number | null>(null);

    const featuredProjects = useMemo(() => sortFeaturedProjects(projects.filter(project => project.isFeatured)), [projects]);
    const availableProjects = useMemo(() => sortByStartDate(projects.filter(project => !project.isFeatured)), [projects]);

    useEffect(() => {
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);

        void loadProjects();

        return () => controller.abort();

        async function loadProjects() {
            try {
                const response = await fetchAdminProjects(controller.signal);

                if (controller.signal.aborted) {
                    return;
                }

                setProjects(response);
            } catch (caughtError) {
                if (controller.signal.aborted) {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load projects right now.');
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false);
                }
            }
        }
    }, []);

    const refreshProjects = useCallback(async () => {
        try {
            const nextProjects = await fetchAdminProjects();
            setProjects(nextProjects);
            setError(null);
        } catch (caughtError) {
            setError(caughtError instanceof Error ? caughtError.message : 'Unable to refresh projects right now.');
            throw caughtError;
        }
    }, []);

    const handleSetFeatured = useCallback(async (projectId: number, isFeatured: boolean) => {
        setInFlightProjectId(projectId);
        setNotice(null);
        setError(null);

        try {
            await setProjectFeaturedState(projectId, isFeatured);
            await refreshProjects();
            setNotice(isFeatured ? 'Project was marked as featured.' : 'Project was removed from featured projects.');
        } catch (caughtError) {
            setError(caughtError instanceof Error ? caughtError.message : 'Unable to update featured state.');
        } finally {
            setInFlightProjectId(null);
        }
    }, [refreshProjects]);

    const handleMove = useCallback(async (index: number, offset: number) => {
        const nextFeatured = [...featuredProjects];
        const destinationIndex = index + offset;

        if (destinationIndex < 0 || destinationIndex >= nextFeatured.length) {
            return;
        }

        setNotice(null);
        setError(null);

        const currentProject = nextFeatured[index];
        nextFeatured[index] = nextFeatured[destinationIndex];
        nextFeatured[destinationIndex] = currentProject;

        setInFlightProjectId(currentProject.id);

        try {
            await setFeaturedProjectOrder(nextFeatured.map(project => project.id));
            await refreshProjects();
            setNotice('Featured project order updated.');
        } catch (caughtError) {
            setError(caughtError instanceof Error ? caughtError.message : 'Unable to reorder featured projects.');
        } finally {
            setInFlightProjectId(null);
        }
    }, [featuredProjects, refreshProjects]);

    return (
        <section className="admin-card admin-section-panel">
            <p className="eyebrow">Project Management</p>
            <h2>Featured project selection and ordering</h2>
            <p>
                Promote projects into the public featured area and reorder them so homepage and featured surfaces match your priority.
            </p>

            {isLoading ? <p className="helper-copy">Loading projects from admin endpoint...</p> : null}
            {error ? <p className="status-banner error">{error}</p> : null}
            {notice ? <p className="status-banner">{notice}</p> : null}

            <div className="admin-project-grid">
                <article className="admin-card">
                    <p className="eyebrow">Featured projects</p>
                    <h3>Current order</h3>

                    {featuredProjects.length === 0 ? (
                        <p className="secondary-copy">No featured projects are currently set.</p>
                    ) : (
                        <ul className="admin-project-list">
                            {featuredProjects.map((project, index) => (
                                <li key={project.id} className="admin-project-item">
                                    <div>
                                        <strong>{project.title}</strong>
                                        <p className="admin-project-meta">
                                            Position {index + 1}
                                            {project.featuredOrder === null || project.featuredOrder === undefined
                                                ? ' (recalculated)'
                                                : ` (order ${project.featuredOrder})`}
                                        </p>
                                    </div>

                                    <div className="admin-project-actions">
                                        <button
                                            className="secondary-action primary-action"
                                            type="button"
                                            disabled={featuredProjects.length < 2 || inFlightProjectId !== null}
                                            onClick={() => handleMove(index, -1)}>
                                            Move up
                                        </button>
                                        <button
                                            className="secondary-action primary-action"
                                            type="button"
                                            disabled={featuredProjects.length < 2 || inFlightProjectId !== null}
                                            onClick={() => handleMove(index, 1)}>
                                            Move down
                                        </button>
                                        <button
                                            className="secondary-action primary-action"
                                            type="button"
                                            disabled={inFlightProjectId !== null}
                                            onClick={() => handleSetFeatured(project.id, false)}>
                                            Unfeature
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </article>

                <article className="admin-card">
                    <p className="eyebrow">Available projects</p>
                    <h3>Add projects to featured</h3>

                    {availableProjects.length === 0 ? (
                        <p className="secondary-copy">No additional projects available to feature.</p>
                    ) : (
                        <ul className="admin-project-list">
                            {availableProjects.map(project => (
                                <li key={project.id} className="admin-project-item">
                                    <div>
                                        <strong>{project.title}</strong>
                                        <p className="admin-project-meta">{project.shortDescription}</p>
                                    </div>

                                    <div className="admin-project-actions">
                                        <button
                                            className="primary-action"
                                            type="button"
                                            disabled={inFlightProjectId !== null}
                                            onClick={() => handleSetFeatured(project.id, true)}>
                                            Feature
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </article>
            </div>
        </section>
    );
}
