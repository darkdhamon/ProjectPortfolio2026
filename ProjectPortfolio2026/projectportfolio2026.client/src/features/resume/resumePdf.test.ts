/// <reference types="node" />
import { readFileSync } from 'fs';
import { resolve } from 'path';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Employer, PortfolioProfile } from '../../app/types';
import { buildResumePdfBytes, buildResumePdfTextLines } from './resumePdf';

const unicodePdfFontBytes = readFileSync(resolve(process.cwd(), 'node_modules', '@fontpkg', 'unifont', 'unifont-15.0.01.ttf'));

function createProfile(overrides: Partial<PortfolioProfile> = {}): PortfolioProfile {
    return {
        id: 1,
        displayName: 'Łukasz Søren',
        contactHeadline: 'Principal engineer focused on resilient delivery.',
        contactIntro: 'Leading payment migrations for Łukasz, Søren, and multilingual platform teams.',
        availabilityHeadline: 'Available for distributed senior product engineering roles.',
        availabilitySummary: 'Open to remote-friendly opportunities with product ownership.',
        contactMethods: [],
        socialLinks: [],
        ...overrides
    };
}

function createEmployer(overrides: Partial<Employer> = {}): Employer {
    return {
        id: 1,
        name: '株式会社サンプル',
        city: 'Tokyo',
        region: 'Tokyo',
        country: 'Japan',
        jobRoles: [
            {
                role: 'Staff Engineer',
                startDate: '2023-01-01',
                endDate: null,
                descriptionMarkdown: 'Scaled multilingual resume exports for global hiring teams.',
                skills: ['Architecture', 'Delivery'],
                technologies: ['React', '.NET 10']
            }
        ],
        ...overrides
    };
}

function mockPdfFontFetch() {
    const fontBytes = unicodePdfFontBytes;
    const fontArrayBuffer = fontBytes.buffer.slice(fontBytes.byteOffset, fontBytes.byteOffset + fontBytes.byteLength);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
        ok: true,
        arrayBuffer: async () => fontArrayBuffer.slice(0)
    }));
}

describe('resumePdf', () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it('preserves unicode resume content in the generated text lines', () => {
        const text = buildResumePdfTextLines({
            profile: createProfile(),
            employers: [createEmployer()],
            activeTimeWindowLabel: 'Last 5 years',
            activeEmployerLimitLabel: 'All employers',
            viewSummary: 'Showing the current filtered resume view.',
            generatedAt: new Date('2026-06-04T12:00:00Z'),
            skillHighlights: ['Architecture'],
            technologyHighlights: ['React']
        }).join('\n');

        expect(text).toContain('Łukasz Søren');
        expect(text).toContain('Leading payment migrations for Łukasz, Søren, and multilingual platform teams.');
        expect(text).toContain('株式会社サンプル');
        expect(text).not.toContain('?ukasz');
        expect(text).not.toContain('S?ren');
        expect(text).not.toContain('????');
    });

    it('creates PDF bytes for unicode resume exports', async () => {
        mockPdfFontFetch();

        const bytes = await buildResumePdfBytes({
            profile: createProfile(),
            employers: [createEmployer()],
            activeTimeWindowLabel: 'Last 5 years',
            activeEmployerLimitLabel: 'All employers',
            viewSummary: 'Showing the current filtered resume view.',
            generatedAt: new Date('2026-06-04T12:00:00Z'),
            skillHighlights: ['Architecture'],
            technologyHighlights: ['React']
        });

        expect(bytes.length).toBeGreaterThan(0);
        expect(new TextDecoder().decode(bytes.slice(0, 8))).toContain('%PDF-');
    });
});
