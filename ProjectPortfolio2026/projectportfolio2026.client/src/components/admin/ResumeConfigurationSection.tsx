import { useEffect, useState, type SyntheticEvent } from 'react';
import {
    fetchAdminResumeConfiguration,
    saveResumeConfigurationAsync,
    type ResumeConfigurationDraft
} from '../../api/resumeConfiguration';
import type { ResumeConfiguration, ResumeSourceType } from '../../app/types';

const emptyDraft: ResumeConfigurationDraft = {
    sourceType: 'none',
    sourceUrl: '',
    displayLabel: '',
    summary: ''
};

function createDraft(configuration: ResumeConfiguration | null): ResumeConfigurationDraft {
    if (!configuration) {
        return emptyDraft;
    }

    return {
        sourceType: configuration.sourceType,
        sourceUrl: configuration.sourceUrl ?? '',
        displayLabel: configuration.displayLabel ?? '',
        summary: configuration.summary ?? ''
    };
}

function validateDraft(draft: ResumeConfigurationDraft) {
    if (draft.sourceType === 'none') {
        return null;
    }

    if (!draft.sourceUrl.trim()) {
        return 'A resume source URL is required when a hosted or embedded source is enabled.';
    }

    try {
        const parsedUrl = new URL(draft.sourceUrl.trim());
        if (!['http:', 'https:'].includes(parsedUrl.protocol)) {
            return 'Resume source URLs must use an absolute http or https address.';
        }
    } catch {
        return 'Resume source URLs must use an absolute http or https address.';
    }

    if (!draft.displayLabel.trim()) {
        return 'A public display label is required when a resume source is enabled.';
    }

    return null;
}

function getSourceTypeSummary(sourceType: ResumeSourceType) {
    return sourceType === 'embed'
        ? 'Embed-style sources stay off-page here and open externally from the public resume.'
        : sourceType === 'hosted-file'
            ? 'Hosted files are exposed as an external resume action from the public page.'
            : 'No external resume source will be shown publicly.';
}

export function ResumeConfigurationSection() {
    const [draft, setDraft] = useState<ResumeConfigurationDraft>(emptyDraft);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [notice, setNotice] = useState<string | null>(null);
    const [isConfigured, setIsConfigured] = useState(false);

    useEffect(() => {
        const controller = new AbortController();

        setIsLoading(true);
        setError(null);

        void loadConfiguration();

        return () => controller.abort();

        async function loadConfiguration() {
            try {
                const configuration = await fetchAdminResumeConfiguration(controller.signal);
                if (controller.signal.aborted) {
                    return;
                }

                setDraft(createDraft(configuration));
                setIsConfigured(configuration?.isConfigured ?? false);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load resume configuration.');
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    async function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const validationError = validateDraft(draft);
        if (validationError) {
            setError(validationError);
            setNotice(null);
            return;
        }

        setIsSaving(true);
        setError(null);
        setNotice(null);

        try {
            const savedConfiguration = await saveResumeConfigurationAsync(draft);
            setDraft(createDraft(savedConfiguration));
            setIsConfigured(savedConfiguration?.isConfigured ?? false);
            setNotice('Resume configuration saved.');
        } catch (caughtError) {
            setError(caughtError instanceof Error ? caughtError.message : 'Unable to save resume configuration.');
        } finally {
            setIsSaving(false);
        }
    }

    const sourceTypeSummary = getSourceTypeSummary(draft.sourceType);

    return (
        <section className="admin-resume-grid">
            <form className="admin-card admin-form" onSubmit={handleSubmit}>
                <p className="eyebrow">Resume Configuration</p>
                <h2>Manage the public resume source</h2>
                <p>
                    Configure the optional external resume action without tying it to broader profile or publishing workflows.
                </p>

                <div className="admin-form-grid">
                    <label className="auth-field">
                        <span>Source type</span>
                        <select
                            value={draft.sourceType}
                            onChange={event => {
                                const nextSourceType = event.target.value as ResumeSourceType;
                                setDraft(current => ({
                                    ...current,
                                    sourceType: nextSourceType
                                }));
                            }}>
                            <option value="none">No external source</option>
                            <option value="hosted-file">Hosted file</option>
                            <option value="embed">Embed source</option>
                        </select>
                    </label>

                    <label className="auth-field">
                        <span>Source URL</span>
                        <input
                            type="url"
                            value={draft.sourceUrl}
                            onChange={event => setDraft(current => ({ ...current, sourceUrl: event.target.value }))}
                            placeholder="https://example.com/resume.pdf"
                            disabled={draft.sourceType === 'none'}
                        />
                    </label>

                    <label className="auth-field">
                        <span>Public label</span>
                        <input
                            type="text"
                            value={draft.displayLabel}
                            onChange={event => setDraft(current => ({ ...current, displayLabel: event.target.value }))}
                            placeholder="Download Resume"
                            disabled={draft.sourceType === 'none'}
                        />
                    </label>

                    <label className="auth-field">
                        <span>Public summary</span>
                        <textarea
                            value={draft.summary}
                            onChange={event => setDraft(current => ({ ...current, summary: event.target.value }))}
                            placeholder="ATS-friendly PDF for recruiters who prefer a file download."
                            rows={4}
                            disabled={draft.sourceType === 'none'}
                        />
                    </label>
                </div>

                {error ? <p className="status-banner error">{error}</p> : null}
                {notice ? <p className="status-banner">{notice}</p> : null}
                {isLoading ? <p className="secondary-copy">Loading saved resume configuration...</p> : null}

                <button className="primary-action" type="submit" disabled={isLoading || isSaving}>
                    {isSaving ? 'Saving Resume Configuration...' : 'Save Resume Configuration'}
                </button>
            </form>

            <aside className="admin-card admin-note-panel">
                <p className="eyebrow">Public Visibility</p>
                <h2>{isConfigured ? 'Visible from the public resume page' : 'Hidden until configuration is complete'}</h2>
                <p>{sourceTypeSummary}</p>

                <div className="admin-note-stack">
                    <div>
                        <span className="stat-label">Current mode</span>
                        <strong>{draft.sourceType === 'none' ? 'No external source' : draft.sourceType === 'embed' ? 'Embed source' : 'Hosted file'}</strong>
                    </div>
                    <div>
                        <span className="stat-label">Label</span>
                        <strong>{draft.displayLabel.trim() || 'Not set yet'}</strong>
                    </div>
                    <div>
                        <span className="stat-label">Summary</span>
                        <strong>{draft.summary.trim() || 'No helper copy yet'}</strong>
                    </div>
                </div>
            </aside>
        </section>
    );
}
