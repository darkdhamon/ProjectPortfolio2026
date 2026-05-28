import { fetchAuthFormData } from './http';

export interface ResumeImportParseResponse {
    requestId?: string;
    sourceFileName?: string | null;
    parserName?: string | null;
    professionalSummary?: string | null;
    rawText?: string | null;
    globalSkills: string[];
    workHistory: ResumeImportWorkHistoryEntry[];
    person?: ResumeImportPerson | null;
}

export interface ResumeImportPerson {
    fullName?: string | null;
    headline?: string | null;
    emailAddress?: string | null;
    phoneNumbers: string[];
    socialProfiles: string[];
}

export interface ResumeImportWorkHistoryEntry {
    employerName?: string | null;
    jobTitle?: string | null;
    descriptionLines: string[];
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
