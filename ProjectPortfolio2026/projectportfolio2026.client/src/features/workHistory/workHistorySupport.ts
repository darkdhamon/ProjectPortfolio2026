import { formatMonth, renderMarkdownParagraphs } from '../../appSupport';

export function formatWorkDateRange(startDate: string, endDate?: string | null) {
    const start = formatMonth(startDate);
    const end = endDate ? formatMonth(endDate) : 'Present';
    return `${start} to ${end}`;
}

export function formatEmployerLocation(city?: string | null, region?: string | null) {
    const parts = [city?.trim(), region?.trim()].filter(part => part && part.length > 0);
    return parts.length > 0 ? parts.join(', ') : 'Location available on resume view';
}

export function renderWorkDescription(markdown: string) {
    return renderMarkdownParagraphs(markdown);
}
