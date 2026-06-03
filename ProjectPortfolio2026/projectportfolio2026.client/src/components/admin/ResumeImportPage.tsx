import { useState, type FormEvent } from 'react';
import { parseResumeUploadAsync, type ResumeImportParseResponse } from '../../api/resumeImport';
import type { NavigateFn } from '../../app/navigation';
import { InternalLink } from '../common/InternalLink';

type ResumeImportSource = 'upload' | 'configured';

function formatCandidateCount(count: number, singularLabel: string, pluralLabel: string) {
    return `${count} ${count === 1 ? singularLabel : pluralLabel}`;
}

export function ResumeImportPage({
    currentUserDisplayName,
    onNavigate,
    onParseUpload = parseResumeUploadAsync
}: {
    currentUserDisplayName: string;
    onNavigate: NavigateFn;
    onParseUpload?: (file: File) => Promise<ResumeImportParseResponse>;
}) {
    const [selectedSource, setSelectedSource] = useState<ResumeImportSource | null>(null);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [isParsing, setIsParsing] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const [parseResult, setParseResult] = useState<ResumeImportParseResponse | null>(null);

    function handleSourceSelection(nextSource: ResumeImportSource) {
        setSelectedSource(nextSource);
        setSelectedFile(null);
        setErrorMessage(null);
        setParseResult(null);
    }

    async function handleUploadSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        if (!selectedFile) {
            setErrorMessage('Choose a PDF or DOCX resume file before starting the import.');
            return;
        }

        setIsParsing(true);
        setErrorMessage(null);

        try {
            const response = await onParseUpload(selectedFile);
            setParseResult(response);
        } catch (caughtError) {
            setParseResult(null);
            setErrorMessage(caughtError instanceof Error ? caughtError.message : 'Unable to start the resume import.');
        } finally {
            setIsParsing(false);
        }
    }

    const parsedEmployerCount = parseResult?.candidateWorkHistory.length ?? 0;
    const parsedWorkHistoryCount = parseResult?.candidateWorkHistory.reduce((count, employer) => count + employer.jobRoles.length, 0) ?? 0;
    const parsedSkillsCount = parseResult?.globalSkills.length ?? 0;
    const parserName = parseResult?.parserName?.trim() || 'Pending parser implementation';

    return (
        <main className="admin-page resume-import-page">
            <section className="admin-hero resume-import-hero">
                <div>
                    <p className="eyebrow">Admin Resume Import</p>
                    <h1>Choose a source for resume import</h1>
                    <p className="hero-description">
                        Start the import from a clean admin workflow, choose where the resume comes from,
                        and keep parsing separate from the later review and approval steps.
                    </p>
                </div>

                <div className="admin-callout">
                    <span className="stat-label">Current admin</span>
                    <strong>{currentUserDisplayName}</strong>
                    <p className="secondary-copy">Source selection lands before candidate review, duplicate checks, and save approval.</p>
                </div>
            </section>

            <section className="admin-grid resume-import-source-grid" aria-label="Resume import source choices">
                <article className={`admin-card resume-import-source-card${selectedSource === 'upload' ? ' selected' : ''}`}>
                    <p className="eyebrow">Source Option</p>
                    <h2>Upload a new resume document</h2>
                    <p>Use a one-off PDF or DOCX file when you want to parse a fresh version without touching longer-lived configuration yet.</p>
                    <ul className="admin-section-list">
                        <li>Recommended for the first MVP import pass.</li>
                        <li>Supported file types stay limited to PDF and DOCX.</li>
                        <li>The parsed result stays in the import workflow instead of updating public content automatically.</li>
                    </ul>
                    <button
                        type="button"
                        className={`resume-import-source-toggle${selectedSource === 'upload' ? ' active' : ''}`}
                        aria-pressed={selectedSource === 'upload'}
                        onClick={() => handleSourceSelection('upload')}>
                        Select upload source
                    </button>
                </article>

                <article className={`admin-card resume-import-source-card${selectedSource === 'configured' ? ' selected' : ''}`}>
                    <p className="eyebrow">Source Option</p>
                    <h2>Use a configured master resume source</h2>
                    <p>Reserve a path for a stored resume file or external master source so recurring imports can skip another manual upload.</p>
                    <ul className="admin-section-list">
                        <li>Issue #67 will define where the master source is configured.</li>
                        <li>Issue #96 will connect this choice to the stored or external source itself.</li>
                        <li>This choice should stay visible now so admins see both planned entry points.</li>
                    </ul>
                    <button
                        type="button"
                        className={`resume-import-source-toggle${selectedSource === 'configured' ? ' active' : ''}`}
                        aria-pressed={selectedSource === 'configured'}
                        onClick={() => handleSourceSelection('configured')}>
                        Select configured source
                    </button>
                </article>
            </section>

            <section className="admin-grid resume-import-workflow-grid">
                {selectedSource === 'upload' ? (
                    <article className="admin-card resume-import-panel">
                        <p className="eyebrow">Step 2</p>
                        <h2>Upload and stage a resume for parsing</h2>
                        <p>Select the document you want to parse. The later review issues will handle editing, duplicate checks, and approval after the parser returns candidate data.</p>

                        <form className="resume-import-file-form" onSubmit={handleUploadSubmit}>
                            <label className="auth-field" htmlFor="resume-import-file">
                                <span>Resume file</span>
                                <input
                                    id="resume-import-file"
                                    name="resumeFile"
                                    type="file"
                                    accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                    onChange={event => {
                                        const nextFile = event.target.files?.[0] ?? null;
                                        setSelectedFile(nextFile);
                                        setErrorMessage(null);
                                        setParseResult(null);
                                    }}
                                />
                            </label>

                            <p className="auth-helper">Supported formats: PDF and DOCX. This step starts parsing but does not save anything into the portfolio yet.</p>

                            <div className="auth-actions">
                                <button type="submit" className="primary-action" disabled={isParsing}>
                                    {isParsing ? 'Parsing Resume...' : 'Parse Uploaded Resume'}
                                </button>
                                <InternalLink
                                    className="admin-action-link"
                                    href="/admin/resume"
                                    onNavigate={onNavigate}
                                    preserveScroll={true}>
                                    Back to Resume Workspace
                                </InternalLink>
                            </div>
                        </form>

                        {errorMessage ? (
                            <p className="resume-import-feedback error-message" role="alert">{errorMessage}</p>
                        ) : null}

                        {parseResult ? (
                            <section className="resume-import-preview" aria-label="Resume import parse summary">
                                <div className="resume-import-preview-heading">
                                    <p className="eyebrow">Parse Summary</p>
                                    <h3>Candidate data is ready for the next import stages</h3>
                                </div>

                                <div className="resume-import-preview-grid">
                                    <article className="resume-import-preview-card">
                                        <span className="stat-label">Source file</span>
                                        <strong>{parseResult.sourceFileName ?? selectedFile?.name ?? 'Uploaded resume'}</strong>
                                        <p>Parser: {parserName}</p>
                                    </article>
                                    <article className="resume-import-preview-card">
                                        <span className="stat-label">Parsed candidates</span>
                                        <strong>{formatCandidateCount(parsedWorkHistoryCount, 'role candidate', 'role candidates')}</strong>
                                        <p>{formatCandidateCount(parsedEmployerCount, 'employer group', 'employer groups')} and {formatCandidateCount(parsedSkillsCount, 'global skill', 'global skills')}</p>
                                    </article>
                                    <article className="resume-import-preview-card">
                                        <span className="stat-label">Detected person</span>
                                        <strong>{parseResult.person?.fullName?.trim() || 'No person name extracted yet'}</strong>
                                        <p>{parseResult.person?.headline?.trim() || 'Profile summary and review UI arrive in later child issues.'}</p>
                                    </article>
                                </div>

                                <p className="resume-import-feedback">
                                    This page starts the import safely. Candidate review, editing controls, duplicate flags,
                                    and save approval continue in later resume-import issues.
                                </p>
                            </section>
                        ) : null}
                    </article>
                ) : selectedSource === 'configured' ? (
                    <article className="admin-card resume-import-panel">
                        <p className="eyebrow">Step 2</p>
                        <h2>Configured source is reserved but not wired yet</h2>
                        <p>
                            The source-selection workflow now exposes the future configured-source path without forcing
                            parsing internals or hidden configuration into the first-step UI.
                        </p>
                        <ul className="admin-section-list">
                            <li>Resume configuration under issue #67 will determine which stored or external source is available.</li>
                            <li>Issue #96 will connect this route to the configured source fetch and parse behavior.</li>
                            <li>Use the upload path today when you need to start an import immediately.</li>
                        </ul>
                        <div className="auth-actions">
                            <button
                                type="button"
                                className="primary-action secondary-action"
                                onClick={() => handleSourceSelection('upload')}>
                                Switch to Upload Source
                            </button>
                            <InternalLink
                                className="admin-action-link"
                                href="/admin/resume"
                                onNavigate={onNavigate}
                                preserveScroll={true}>
                                Back to Resume Workspace
                            </InternalLink>
                        </div>
                    </article>
                ) : (
                    <article className="admin-card resume-import-panel">
                        <p className="eyebrow">Step 2</p>
                        <h2>Select a source to continue</h2>
                        <p>Choose whether this import starts from an upload or a configured source before any parsing begins.</p>
                        <ul className="admin-section-list">
                            <li>The upload option is ready to stage a new document.</li>
                            <li>The configured-source option stays visible so the admin workflow reflects the long-term import direction.</li>
                            <li>Nothing is saved automatically after this step.</li>
                        </ul>
                    </article>
                )}

                <article className="admin-card resume-import-panel">
                    <p className="eyebrow">Workflow Boundary</p>
                    <h2>What this issue covers</h2>
                    <p>
                        This work establishes the admin entry point and source selection flow. It deliberately stops before
                        duplicate detection, candidate editing, approval, and persistence so each child issue keeps a clean boundary.
                    </p>
                    <ul className="admin-section-list">
                        <li>#89 starts the import and lets admins pick the source.</li>
                        <li>#91 handles the upload staging details behind the parse endpoint.</li>
                        <li>#94 and #95 pick up once candidate review and approval need UI and persistence.</li>
                    </ul>
                    <div className="auth-actions">
                        <InternalLink
                            className="admin-action-link"
                            href="/admin/resume"
                            onNavigate={onNavigate}
                            preserveScroll={true}>
                            Return to Resume Workspace
                        </InternalLink>
                        <InternalLink
                            className="admin-action-link"
                            href="/admin"
                            onNavigate={onNavigate}
                            preserveScroll={true}>
                            Return to Overview
                        </InternalLink>
                    </div>
                </article>
            </section>
        </main>
    );
}
