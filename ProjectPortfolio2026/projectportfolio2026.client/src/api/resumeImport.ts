import { fetchAuthFormData } from './http';
import { fetchAuthJson } from './http';

export interface ResumeImportParseResponse {
    requestId?: string;
    sourceFileName?: string | null;
    parserName?: string | null;
    professionalSummary?: string | null;
    rawText?: string | null;
    globalSkills: string[];
    candidateWorkHistory: ResumeImportEmployerCandidate[];
    person?: ResumeImportPerson | null;
}

export interface ResumeImportPerson {
    fullName?: string | null;
    headline?: string | null;
    emailAddress?: string | null;
    phoneNumbers: string[];
    socialProfiles: string[];
}

export interface ResumeImportEmployerCandidate {
    candidateId: string;
    employerName?: string | null;
    jobRoles: ResumeImportJobRoleCandidate[];
}

export interface ResumeImportJobRoleCandidate {
    candidateId: string;
    jobTitle?: string | null;
    descriptionLines: string[];
    descriptionMarkdown?: string | null;
    skills: string[];
    technologies: string[];
}

export async function parseResumeUploadAsync(file: File) {
    const formData = new FormData();
    formData.set('file', file);

    const { payload } = await fetchAuthFormData<ResumeImportParseResponse>(
        '/api/admin/resume-import/parse',
        {
            method: 'POST',
            body: formData
        },
        'Unable to parse the selected resume file.'
    );

    if (!payload) {
        throw new Error('The resume import service returned an empty response.');
    }

    return payload as ResumeImportParseResponse;
}

export async function parseResumeConfiguredAsync() {
    const { payload } = await fetchAuthJson<ResumeImportParseResponse>(
        '/api/admin/resume-import/parse-configured',
        {
            method: 'POST'
        },
        'Unable to parse the configured resume source.'
    );

    if (!payload) {
        throw new Error('The resume import service returned an empty response.');
    }

    return payload as ResumeImportParseResponse;
}
