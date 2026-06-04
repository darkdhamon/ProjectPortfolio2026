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

function sanitizePdfText(value: string) {
    return value
        .normalize('NFKD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/\u2013|\u2014/g, '-')
        .replace(/\u2018|\u2019/g, "'")
        .replace(/\u201c|\u201d/g, '"')
        .replace(/\u2022/g, '*')
        .replace(/\u2026/g, '...')
        .replace(/\u00a0/g, ' ')
        .replace(/[^\x20-\x7E]/g, '?');
}

function escapePdfText(value: string) {
    return value
        .replace(/\\/g, '\\\\')
        .replace(/\(/g, '\\(')
        .replace(/\)/g, '\\)');
}

function wrapPdfText(text: string, fontSize: number, indent = 0) {
    const availableWidth = pdfPageWidth - (pdfMargin * 2) - indent;
    const approximateCharacterWidth = Math.max(4, fontSize * 0.52);
    const maxCharacters = Math.max(18, Math.floor(availableWidth / approximateCharacterWidth));
    const sanitized = sanitizePdfText(text).trim();

    if (!sanitized) {
        return [''];
    }

    const words = sanitized.split(/\s+/);
    const lines: string[] = [];
    let currentLine = '';

    for (const word of words) {
        const nextLine = currentLine ? `${currentLine} ${word}` : word;
        if (nextLine.length <= maxCharacters) {
            currentLine = nextLine;
            continue;
        }

        if (currentLine) {
            lines.push(currentLine);
        }

        if (word.length <= maxCharacters) {
            currentLine = word;
            continue;
        }

        let remainingWord = word;
        while (remainingWord.length > maxCharacters) {
            lines.push(remainingWord.slice(0, maxCharacters - 1));
            remainingWord = remainingWord.slice(maxCharacters - 1);
        }
        currentLine = remainingWord;
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
    return sanitizePdfText(
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

export function buildResumePdfBytes(options: ResumePdfExportOptions) {
    const lines = buildPdfLines(options);
    const pages: string[][] = [[]];
    let currentPageIndex = 0;
    let currentY = pdfPageHeight - pdfMargin;

    for (const line of lines) {
        const lineHeight = line.text ? line.fontSize + 6 : line.fontSize;
        if (currentY - lineHeight < pdfBottomMargin) {
            pages.push([]);
            currentPageIndex += 1;
            currentY = pdfPageHeight - pdfMargin;
        }

        if (line.text) {
            const x = pdfMargin + (line.indent ?? 0);
            const escapedText = escapePdfText(line.text);
            pages[currentPageIndex].push(`BT /F1 ${line.fontSize} Tf 1 0 0 1 ${x} ${currentY} Tm (${escapedText}) Tj ET`);
        }

        currentY -= lineHeight;
    }

    const objects: string[] = [];
    const pageObjectNumbers: number[] = [];
    const contentObjectNumbers: number[] = [];
    const fontObjectNumber = 3;

    objects[1] = '<< /Type /Catalog /Pages 2 0 R >>';
    objects[2] = '';
    objects[fontObjectNumber] = '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>';

    let nextObjectNumber = 4;
    pages.forEach(pageCommands => {
        const pageObjectNumber = nextObjectNumber++;
        const contentObjectNumber = nextObjectNumber++;
        pageObjectNumbers.push(pageObjectNumber);
        contentObjectNumbers.push(contentObjectNumber);

        const stream = pageCommands.join('\n');
        objects[pageObjectNumber] = `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pdfPageWidth} ${pdfPageHeight}] /Resources << /Font << /F1 ${fontObjectNumber} 0 R >> >> /Contents ${contentObjectNumber} 0 R >>`;
        objects[contentObjectNumber] = `<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`;
    });

    objects[2] = `<< /Type /Pages /Kids [${pageObjectNumbers.map(number => `${number} 0 R`).join(' ')}] /Count ${pageObjectNumbers.length} >>`;

    const parts: string[] = ['%PDF-1.4'];
    const offsets: number[] = [0];

    for (let objectNumber = 1; objectNumber < objects.length; objectNumber += 1) {
        offsets[objectNumber] = parts.join('\n').length + 1;
        parts.push(`${objectNumber} 0 obj\n${objects[objectNumber]}\nendobj`);
    }

    const xrefOffset = parts.join('\n').length + 1;
    const xrefEntries = ['0000000000 65535 f '];

    for (let objectNumber = 1; objectNumber < objects.length; objectNumber += 1) {
        xrefEntries.push(`${offsets[objectNumber].toString().padStart(10, '0')} 00000 n `);
    }

    parts.push(`xref\n0 ${objects.length}\n${xrefEntries.join('\n')}`);
    parts.push(`trailer\n<< /Size ${objects.length} /Root 1 0 R >>`);
    parts.push(`startxref\n${xrefOffset}`);
    parts.push('%%EOF');

    return new TextEncoder().encode(parts.join('\n'));
}

function buildDownloadFileName(displayName: string, timeWindowLabel: string, employerLimitLabel: string) {
    const normalizedName = sanitizePdfText(displayName)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        || 'resume';
    const normalizedTimeWindow = sanitizePdfText(timeWindowLabel)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');
    const normalizedEmployerLimit = sanitizePdfText(employerLimitLabel)
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');

    return [normalizedName, normalizedTimeWindow, normalizedEmployerLimit, 'resume.pdf']
        .filter(Boolean)
        .join('-');
}

export function downloadResumePdf(options: ResumePdfExportOptions) {
    if (typeof document === 'undefined' || typeof Blob === 'undefined' || typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') {
        throw new Error('This browser does not support PDF downloads.');
    }

    const bytes = buildResumePdfBytes(options);
    const blob = new Blob([bytes], {
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
