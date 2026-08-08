import { fetchAuthJson } from './http';

export interface AdminPortfolioSocialLink {
    id?: number | null;
    platform: string;
    label: string;
    url: string;
    handle?: string | null;
    summary?: string | null;
    sortOrder: number;
    isVisible: boolean;
}

export interface AdminSocialLinksPayload {
    socialLinks: AdminPortfolioSocialLink[];
}

export async function fetchAdminSocialLinks(signal?: AbortSignal) {
    const { payload } = await fetchAuthJson<AdminPortfolioSocialLink[]>(
        '/api/admin/social-links',
        {
            method: 'GET',
            signal
        },
        'Unable to load social links.'
    );

    if (!Array.isArray(payload)) {
        throw new Error('Unable to load social links.');
    }

    return payload;
}

export async function saveAdminSocialLinks(links: AdminPortfolioSocialLink[]) {
    const payload: AdminSocialLinksPayload = {
        socialLinks: links.map((link, index) => ({
            id: link.id,
            platform: link.platform,
            label: link.label,
            url: link.url,
            handle: link.handle,
            summary: link.summary,
            sortOrder: index + 1,
            isVisible: link.isVisible
        }))
    };

    const response = await fetchAuthJson<AdminPortfolioSocialLink[]>(
        '/api/admin/social-links',
        {
            method: 'PUT',
            body: JSON.stringify(payload)
        },
        'Unable to save social links.'
    );

    if (!Array.isArray(response.payload)) {
        throw new Error('Unable to save social links.');
    }

    return response.payload;
}
