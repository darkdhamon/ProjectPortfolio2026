import { useEffect, useState, type ChangeEvent } from 'react';
import {
    fetchAdminSocialLinks,
    saveAdminSocialLinks,
    type AdminPortfolioSocialLink
} from '../../api/socialLinks';

interface EditableSocialLink {
    id: number | null;
    platform: string;
    label: string;
    url: string;
    handle: string;
    summary: string;
    sortOrder: number;
    isVisible: boolean;
    localId: string;
}

type FormErrorsByLocalId = Record<string, { platform?: string; label?: string; url?: string }>;

const emptyLink: Omit<EditableSocialLink, 'localId'> = {
    id: null,
    platform: '',
    label: '',
    url: '',
    handle: '',
    summary: '',
    sortOrder: 0,
    isVisible: true
};

const maxPlatformLength = 50;
const maxLabelLength = 100;
const maxUrlLength = 500;
const maxHandleLength = 150;
const maxSummaryLength = 500;

function createRow(localId: string, draft: AdminPortfolioSocialLink | Omit<EditableSocialLink, 'localId'>, sortOrder: number): EditableSocialLink {
    return {
        localId,
        id: draft.id ?? null,
        platform: draft.platform ?? '',
        label: draft.label ?? '',
        url: draft.url ?? '',
        handle: draft.handle ?? '',
        summary: draft.summary ?? '',
        sortOrder,
        isVisible: draft.isVisible
    };
}

function getNextSortOrder(links: EditableSocialLink[]) {
    return links.length + 1;
}

function normalizeDrafts(drafts: AdminPortfolioSocialLink[]) {
    return drafts
        .sort((left, right) => left.sortOrder - right.sortOrder || left.label.localeCompare(right.label))
        .map((draft, index) => createRow(`server-${draft.id ?? index}`, draft, index + 1));
}

function validateDrafts(drafts: EditableSocialLink[]) {
    const errorsByLocalId: FormErrorsByLocalId = {};
    let hasErrors = false;

    for (const draft of drafts) {
        const currentErrors: { platform?: string; label?: string; url?: string } = {};

        if (!draft.platform.trim()) {
            currentErrors.platform = 'Platform is required.';
        }

        if (!draft.label.trim()) {
            currentErrors.label = 'Label is required.';
        }

        try {
            const parsed = new URL(draft.url.trim());
            if (!['http:', 'https:'].includes(parsed.protocol)) {
                currentErrors.url = 'Use an absolute http/https URL.';
            }
        } catch {
            currentErrors.url = 'Use an absolute http/https URL.';
        }

        if (Object.keys(currentErrors).length > 0) {
            errorsByLocalId[draft.localId] = currentErrors;
            hasErrors = true;
        }
    }

    return { errorsByLocalId, hasErrors };
}

export function SocialLinksSection() {
    const [links, setLinks] = useState<EditableSocialLink[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [notice, setNotice] = useState<string | null>(null);
    const [rowErrors, setRowErrors] = useState<FormErrorsByLocalId>({});
    const [nextLocalId, setNextLocalId] = useState(1);

    useEffect(() => {
        const controller = new AbortController();
        void loadSocialLinks();

        return () => controller.abort();

        async function loadSocialLinks() {
            setIsLoading(true);
            setError(null);
            setNotice(null);
            setRowErrors({});

            try {
                const socialLinks = await fetchAdminSocialLinks(controller.signal);
                const normalized = socialLinks.length > 0
                    ? normalizeDrafts(socialLinks)
                    : [createRow('local-1', emptyLink, 1)];

                setLinks(normalized);
                setNextLocalId(socialLinks.length + 2);
            } catch (caughtError) {
                if ((caughtError as Error).name === 'AbortError') {
                    return;
                }

                setError(caughtError instanceof Error ? caughtError.message : 'Unable to load social links.');
            } finally {
                setIsLoading(false);
            }
        }
    }, []);

    function updateLink(localId: string, field: keyof Omit<EditableSocialLink, 'localId' | 'id'>, value: string | boolean) {
        setLinks(current => current.map(link => {
            if (link.localId !== localId) {
                return link;
            }

            if (field === 'isVisible' && typeof value === 'boolean') {
                return {
                    ...link,
                    isVisible: value
                };
            }

            return {
                ...link,
                [field]: String(value)
            };
        }));
    }

    function addNewLink() {
        const newLink = createRow(`local-${nextLocalId}`, emptyLink, getNextSortOrder(links));
        setNextLocalId(current => current + 1);
        setLinks(current => [...current, newLink]);
    }

    function removeLink(localId: string) {
        setLinks(current => {
            const nextLinks = current.filter(link => link.localId !== localId);
            return nextLinks.length > 0
                ? nextLinks.map((link, index) => ({ ...link, sortOrder: index + 1 }))
                : [createRow('local-1', emptyLink, 1)];
        });
    }

    function moveLink(localId: string, direction: -1 | 1) {
        setLinks(current => {
            const currentIndex = current.findIndex(link => link.localId === localId);
            const targetIndex = currentIndex + direction;
            if (currentIndex < 0 || targetIndex < 0 || targetIndex >= current.length) {
                return current;
            }

            const nextLinks = [...current];
            [nextLinks[currentIndex], nextLinks[targetIndex]] = [nextLinks[targetIndex], nextLinks[currentIndex]];

            return nextLinks.map((link, index) => ({ ...link, sortOrder: index + 1 }));
        });
    }

    async function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const { errorsByLocalId, hasErrors } = validateDrafts(links);
        if (hasErrors) {
            setError('Fix the highlighted validation issues before saving.');
            setRowErrors(errorsByLocalId);
            setNotice(null);
            return;
        }

        setIsSaving(true);
        setError(null);
        setNotice(null);
        setRowErrors({});

        const payload = links.map((link, index) => ({
            id: link.id,
            platform: link.platform.trim(),
            label: link.label.trim(),
            url: link.url.trim(),
            handle: link.handle.trim() || null,
            summary: link.summary.trim() || null,
            sortOrder: index + 1,
            isVisible: link.isVisible
        })) as AdminPortfolioSocialLink[];

        try {
            const savedLinks = await saveAdminSocialLinks(payload);
            setLinks(normalizeDrafts(savedLinks));
            setNotice('Social links saved.');
        } catch (caughtError) {
            setError(caughtError instanceof Error ? caughtError.message : 'Unable to save social links.');
        } finally {
            setIsSaving(false);
        }
    }

    return (
        <section className="admin-card admin-resume-grid">
            <form className="admin-card admin-form" onSubmit={handleSubmit}>
                <p className="eyebrow">Social Links</p>
                <h2>Manage social links</h2>
                <p>Create, edit, reorder, and hide social links used on the public contact profile.</p>

                <div className="admin-form-grid">
                    {links.map((link, index) => (
                        <section key={link.localId} className="admin-card">
                            <div className="admin-inline-actions">
                                <p className="meta-label">Social link {index + 1}</p>
                                <div className="admin-inline-actions">
                                    <button
                                        type="button"
                                        className="admin-action-link"
                                        onClick={() => moveLink(link.localId, -1)}
                                        disabled={index === 0 || isSaving}
                                    >
                                        Move up
                                    </button>
                                    <button
                                        type="button"
                                        className="admin-action-link"
                                        onClick={() => moveLink(link.localId, 1)}
                                        disabled={index === links.length - 1 || isSaving}
                                    >
                                        Move down
                                    </button>
                                    <button
                                        type="button"
                                        className="admin-action-link"
                                        onClick={() => removeLink(link.localId)}
                                        disabled={isSaving}
                                    >
                                        Remove
                                    </button>
                                </div>
                            </div>

                            <label className="auth-field">
                                <span>Platform</span>
                                <input
                                    type="text"
                                    value={link.platform}
                                    maxLength={maxPlatformLength}
                                    onChange={(event: ChangeEvent<HTMLInputElement>) => updateLink(link.localId, 'platform', event.target.value)}
                                    disabled={isSaving}
                                    aria-label={`Platform ${index + 1}`}
                                />
                                {rowErrors[link.localId]?.platform ? (
                                    <p className="status-banner error">{rowErrors[link.localId]?.platform}</p>
                                ) : null}
                            </label>

                            <label className="auth-field">
                                <span>Label</span>
                                <input
                                    type="text"
                                    value={link.label}
                                    maxLength={maxLabelLength}
                                    onChange={(event: ChangeEvent<HTMLInputElement>) => updateLink(link.localId, 'label', event.target.value)}
                                    disabled={isSaving}
                                    aria-label={`Label ${index + 1}`}
                                />
                                {rowErrors[link.localId]?.label ? (
                                    <p className="status-banner error">{rowErrors[link.localId]?.label}</p>
                                ) : null}
                            </label>

                            <label className="auth-field">
                                <span>URL</span>
                                <input
                                    type="url"
                                    value={link.url}
                                    maxLength={maxUrlLength}
                                    onChange={(event: ChangeEvent<HTMLInputElement>) => updateLink(link.localId, 'url', event.target.value)}
                                    disabled={isSaving}
                                    aria-label={`URL ${index + 1}`}
                                />
                                {rowErrors[link.localId]?.url ? (
                                    <p className="status-banner error">{rowErrors[link.localId]?.url}</p>
                                ) : null}
                            </label>

                            <label className="auth-field">
                                <span>Handle</span>
                                <input
                                    type="text"
                                    value={link.handle}
                                    maxLength={maxHandleLength}
                                    onChange={(event: ChangeEvent<HTMLInputElement>) => updateLink(link.localId, 'handle', event.target.value)}
                                    disabled={isSaving}
                                    aria-label={`Handle ${index + 1}`}
                                />
                            </label>

                            <label className="auth-field">
                                <span>Summary</span>
                                <textarea
                                    value={link.summary}
                                    maxLength={maxSummaryLength}
                                    onChange={(event: ChangeEvent<HTMLTextAreaElement>) => updateLink(link.localId, 'summary', event.target.value)}
                                    disabled={isSaving}
                                    aria-label={`Summary ${index + 1}`}
                                    rows={3}
                                />
                            </label>

                            <label className="auth-field">
                                <span>Visible</span>
                                <input
                                    type="checkbox"
                                    checked={link.isVisible}
                                    onChange={(event: ChangeEvent<HTMLInputElement>) => {
                                        const nextValue = event.target.checked;
                                        setLinks(current => current.map(currentLink => currentLink.localId === link.localId
                                            ? { ...currentLink, isVisible: nextValue }
                                            : currentLink));
                                    }}
                                    disabled={isSaving}
                                />
                            </label>
                        </section>
                    ))}
                </div>

                <div className="admin-inline-actions">
                    <button
                        type="button"
                        className="admin-action-link"
                        onClick={addNewLink}
                        disabled={isSaving}
                    >
                        Add social link
                    </button>
                    <button className="admin-action-link" type="button" onClick={() => setLinks([])} disabled={isSaving}>
                        Clear draft
                    </button>
                </div>

                {error ? <p className="status-banner error">{error}</p> : null}
                {notice ? <p className="status-banner">{notice}</p> : null}
                {isLoading ? <p className="secondary-copy">Loading social links...</p> : null}

                <button className="primary-action" type="submit" disabled={isLoading || isSaving}>
                    {isSaving ? 'Saving social links...' : 'Save social links'}
                </button>
            </form>

            <aside className="admin-card admin-note-panel">
                <p className="eyebrow">Public visibility</p>
                <h2>Visibility is explicit per link</h2>
                <p>Use the visibility toggle to hide incomplete or legacy entries from public pages while keeping them in the draft list.</p>
                <div className="admin-note-stack">
                    <div>
                        <span className="stat-label">Editable links</span>
                        <strong>{links.length}</strong>
                    </div>
                    <div>
                        <span className="stat-label">Visible links</span>
                        <strong>{links.filter(link => link.isVisible).length}</strong>
                    </div>
                    <div>
                        <span className="stat-label">Current order mode</span>
                        <strong>{isSaving ? 'Saving now' : 'Manual ordering'}</strong>
                    </div>
                </div>
            </aside>
        </section>
    );
}
