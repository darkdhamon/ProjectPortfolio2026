import fontkit from '@pdf-lib/fontkit';
import { PDFDocument } from 'pdf-lib';
import unicodeFontUrl from '@fontpkg/unifont/unifont-15.0.01.ttf?url';
import type { Employer, PortfolioContactMethod, PortfolioProfile, PortfolioSocialLink } from '../../app/types';

export interface ResumePdfExportOptions {
    profile: PortfolioProfile | null;
    employers: Employer[];
    activeTimeWindowLabel: string;
    activeEmployerLimitLabel: string;
    viewSummary: string;
    generatedAt: Date;
    skillHighlights: string[];
    technologyHighlights: string[];
}

interface PdfLine {
    text: string;
    fontSize: number;
    indent?: number;
}

const pdfPageWidth = 612;
const pdfPageHeight = 792;
const pdfMargin = 54;
const pdfBottomMargin = 54;

let unicodePdfFontBytesPromise: Promise<Uint8Array> | null = null;

function normalizePdfText(value: string) {
    return value
        .normalize('NFC')
        .replace(/\u2013|\u2014/g, '-')
        .replace(/\u2018|\u2019/g, "'")
        .replace(/\u201c|\u201d/g, '"')
        .replace(/\u2022/g, '*')
        .replace(/\u2026/g, '...')
        .replace(/\u00a0/g, ' ');
}

function sanitizePdfFileNamePart(value: string) {
    return normalizePdfText(value)
        .normalize('NFKD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^\x20-\x7E]/g, '')
        .trim();
}

function getPdfTextLength(value: string) {
    return Array.from(value).length;
}

function splitPdfWord(value: string, maxCharacters: number) {
    const codePoints = Array.from(value);
    const segments: string[] = [];

    for (let index = 0; index < codePoints.length; index += maxCharacters) {
        segments.push(codePoints.slice(index, index + maxCharacters).join(''));
    }

    return segments;
}

function wrapPdfText(text: string, fontSize: number, indent = 0) {
    const availableWidth = pdfPageWidth - (pdfMargin * 2) - indent;
    const approximateCharacterWidth = Math.max(4, fontSize * 0.52);
    const maxCharacters = Math.max(18, Math.floor(availableWidth / approximateCharacterWidth));
    const sanitized = normalizePdfText(text).trim();

    if (!sanitized) {
        return [''];
    }

    const words = sanitized.split(/\s+/);
    const lines: string[] = [];
    let currentLine = '';

    for (const word of words) {
        const nextLine = currentLine ? `${currentLine} ${word}` : word;
        if (getPdfTextLength(nextLine) <= maxCharacters) {
            currentLine = nextLine;
            continue;
        }

        if (currentLine) {
            lines.push(currentLine);
        }

        if (getPdfTextLength(word) <= maxCharacters) {
            currentLine = word;
            continue;
        }

        const segments = splitPdfWord(word, maxCharacters);
        while (segments.length > 1) {
            lines.push(segments.shift() ?? '');
        }
        currentLine = segments[0] ?? '';
    }

    if (currentLine) {
        lines.push(currentLine);
    }

    return lines;
}

function pushWrappedLine(lines: PdfLine[], text: string, fontSize: number, indent = 0) {
    wrapPdfText(text, fontSize, indent).forEach(line => {
        lines.push({
            text: line,
            fontSize,
            indent
        });
    });
}

function pushBlankLine(lines: PdfLine[], fontSize = 10) {
    lines.push({
        text: '',
        fontSize
    });
}

function formatGeneratedDate(value: Date) {
    return value.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

function getContactHref(method: PortfolioContactMethod) {
    if (method.href?.trim()) {
        return method.href.trim();
    }

    if (method.value.includes('@')) {
        return `mailto:${method.value}`;
    }

    if (method.value.startsWith('http://') || method.value.startsWith('https://')) {
        return method.value;
    }

    return null;
}

function formatEmployerLocation(employer: Employer) {
    const parts = [employer.city?.trim(), employer.region?.trim(), employer.country?.trim()].filter(
        (part): part is string => Boolean(part && part.length > 0)
    );

    return parts.join(', ');
}

function formatRoleDate(startDate: string, endDate?: string | null) {
    const start = new Date(`${startDate}T12:00:00`);
    const end = endDate ? new Date(`${endDate}T12:00:00`) : null;
    const formatter = new Intl.DateTimeFormat('en-US', {
        month: 'short',
        year: 'numeric'
    });

    return `${formatter.format(start)} to ${end ? formatter.format(end) : 'Present'}`;
}

function summarizeRoleCopy(markdown: string) {
    return normalizePdfText(
        markdown
            .replace(/^#+\s*/gm, '')
            .split(/\r?\n\r?\n/)
            .map(paragraph => paragraph.trim())
            .find(paragraph => paragraph.length > 0) ?? ''
    );
}

function formatLinkLine(method: PortfolioContactMethod) {
    const href = getContactHref(method);
    const parts = [`${method.label}: ${method.value}`];
    if (href && href !== method.value) {
        parts.push(`(${href})`);
    }

    if (method.note?.trim()) {
        parts.push(`- ${method.note.trim()}`);
    }

    return parts.join(' ');
}

function formatSocialLine(link: PortfolioSocialLink) {
    const parts = [`${link.label}: ${link.handle?.trim() || link.url}`];
    if (link.handle?.trim()) {
        parts.push(`(${link.url})`);
    }

    if (link.summary?.trim()) {
        parts.push(`- ${link.summary.trim()}`);
    }

    return parts.join(' ');
}

function buildPdfLines(options: ResumePdfExportOptions) {
    const lines: PdfLine[] = [];
    const displayName = options.profile?.displayName?.trim() || 'Portfolio Resume';
    const headline = options.profile?.contactHeadline?.trim();
    const availability = options.profile?.availabilityHeadline?.trim();
    const summaryParts = [options.profile?.contactIntro?.trim(), options.profile?.availabilitySummary?.trim()].filter(
        (part): part is string => Boolean(part && part.length > 0)
    );

    pushWrappedLine(lines, displayName, 18);
    if (headline) {
        pushWrappedLine(lines, headline, 12);
    }
    if (availability) {
        pushWrappedLine(lines, availability, 10);
    }
    pushWrappedLine(lines, `Generated ${formatGeneratedDate(options.generatedAt)}`, 9);
    pushBlankLine(lines, 10);

    pushWrappedLine(lines, 'Current Resume View', 13);
    pushWrappedLine(lines, `Time Window: ${options.activeTimeWindowLabel}`, 10);
    pushWrappedLine(lines, `Employer Limit: ${options.activeEmployerLimitLabel}`, 10);
    pushWrappedLine(lines, options.viewSummary, 10);
    pushBlankLine(lines, 10);

    if (options.profile) {
        const contactMethods = options.profile.contactMethods.slice(0, 6);
        const socialLinks = options.profile.socialLinks.slice(0, 4);

        if (contactMethods.length > 0 || socialLinks.length > 0) {
            pushWrappedLine(lines, 'Contact', 13);
            contactMethods.forEach(method => pushWrappedLine(lines, formatLinkLine(method), 10, 14));
            socialLinks.forEach(link => pushWrappedLine(lines, formatSocialLine(link), 10, 14));
            pushBlankLine(lines, 10);
        }
    }

    if (summaryParts.length > 0) {
        pushWrappedLine(lines, 'Summary', 13);
        summaryParts.forEach(paragraph => pushWrappedLine(lines, paragraph, 10));
        pushBlankLine(lines, 10);
    }

    if (options.skillHighlights.length > 0 || options.technologyHighlights.length > 0) {
        pushWrappedLine(lines, 'Highlighted Skills', 13);
        if (options.skillHighlights.length > 0) {
            pushWrappedLine(lines, `Skills: ${options.skillHighlights.join(', ')}`, 10);
        }
        if (options.technologyHighlights.length > 0) {
            pushWrappedLine(lines, `Technologies: ${options.technologyHighlights.join(', ')}`, 10);
        }
        pushBlankLine(lines, 10);
    }

    pushWrappedLine(lines, 'Experience', 13);
    if (options.employers.length === 0) {
        pushWrappedLine(lines, 'No published work history matches the current resume filters.', 10);
        return lines;
    }

    options.employers.forEach(employer => {
        const location = formatEmployerLocation(employer);
        pushWrappedLine(lines, employer.name, 12);
        if (location) {
            pushWrappedLine(lines, location, 10, 14);
        }

        employer.jobRoles.forEach(role => {
            pushWrappedLine(lines, `${role.role} | ${formatRoleDate(role.startDate, role.endDate)}`, 10, 14);
            const summary = summarizeRoleCopy(role.descriptionMarkdown);
            if (summary) {
                pushWrappedLine(lines, summary, 10, 28);
            }

            if (role.skills.length > 0) {
                pushWrappedLine(lines, `Skills: ${role.skills.join(', ')}`, 9, 28);
            }

            if (role.technologies.length > 0) {
                pushWrappedLine(lines, `Technologies: ${role.technologies.join(', ')}`, 9, 28);
            }
        });

        pushBlankLine(lines, 10);
    });

    return lines;
}

export function buildResumePdfTextLines(options: ResumePdfExportOptions) {
    return buildPdfLines(options)
        .map(line => line.text)
        .filter(text => text.length > 0);
}

async function loadUnicodePdfFontBytes() {
    if (!unicodePdfFontBytesPromise) {
        unicodePdfFontBytesPromise = fetch(unicodeFontUrl)
            .then(async response => {
                if (!response.ok) {
                    throw new Error('Unable to load the PDF export font.');
                }

                return new Uint8Array(await response.arrayBuffer());
            })
            .catch(error => {
                unicodePdfFontBytesPromise = null;
                throw error;
            });
    }

    return unicodePdfFontBytesPromise;
}

export async function buildResumePdfBytes(options: ResumePdfExportOptions) {
    const lines = buildPdfLines(options);
    const pdfDocument = await PDFDocument.create();
    pdfDocument.registerFontkit(fontkit);
    const font = await pdfDocument.embedFont(await loadUnicodePdfFontBytes(), {
        subset: true
    });
    let page = pdfDocument.addPage([pdfPageWidth, pdfPageHeight]);
    let currentY = pdfPageHeight - pdfMargin;

    for (const line of lines) {
        const lineHeight = line.text ? line.fontSize + 6 : line.fontSize;
        if (currentY - lineHeight < pdfBottomMargin) {
            page = pdfDocument.addPage([pdfPageWidth, pdfPageHeight]);
            currentY = pdfPageHeight - pdfMargin;
        }

        if (line.text) {
            page.drawText(line.text, {
                font,
                size: line.fontSize,
                x: pdfMargin + (line.indent ?? 0),
                y: currentY
            });
        }

        currentY -= lineHeight;
    }

    return pdfDocument.save({
        useObjectStreams: false
    });
}

function buildDownloadFileName(displayName: string, timeWindowLabel: string, employerLimitLabel: string) {
    const normalizedName = sanitizePdfFileNamePart(displayName)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        || 'resume';
    const normalizedTimeWindow = sanitizePdfFileNamePart(timeWindowLabel)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');
    const normalizedEmployerLimit = sanitizePdfFileNamePart(employerLimitLabel)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');

    return [normalizedName, normalizedTimeWindow, normalizedEmployerLimit, 'resume.pdf']
        .filter(Boolean)
        .join('-');
}

export async function downloadResumePdf(options: ResumePdfExportOptions) {
    if (typeof document === 'undefined'
        || typeof Blob === 'undefined'
        || typeof URL === 'undefined'
        || typeof URL.createObjectURL !== 'function'
        || typeof fetch !== 'function') {
        throw new Error('This browser does not support PDF downloads.');
    }

    const bytes = await buildResumePdfBytes(options);
    const blobBytes = new Uint8Array(bytes.byteLength);
    blobBytes.set(bytes);
    const blob = new Blob([blobBytes], {
        type: 'application/pdf'
    });
    const downloadUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    const displayName = options.profile?.displayName?.trim() || 'resume';

    link.href = downloadUrl;
    link.download = buildDownloadFileName(displayName, options.activeTimeWindowLabel, options.activeEmployerLimitLabel);
    link.rel = 'noreferrer';
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(downloadUrl);
}
