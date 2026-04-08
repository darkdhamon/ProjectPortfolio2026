import { type AccountDraft, type AuthUser } from '../components/admin/mockAuth';
import { fetchAuthJson } from './http';

export interface AuthStatusResponse {
    isAuthenticated: boolean;
    isAdmin: boolean;
    userId?: string | null;
    userName?: string | null;
    email?: string | null;
    displayName?: string | null;
}

export function mapAuthUser(response: AuthStatusResponse): AuthUser | null {
    if (!response.isAuthenticated || !response.userName) {
        return null;
    }

    return {
        username: response.userName,
        email: response.email ?? null,
        displayName: response.displayName?.trim() || response.userName,
        isAdmin: response.isAdmin
    };
}

export async function fetchCurrentUser(signal?: AbortSignal) {
    const { payload } = await fetchAuthJson<AuthStatusResponse>(
        '/api/auth/me',
        {
            method: 'GET',
            signal
        },
        'Unable to check the current session.'
    );

    return payload ? mapAuthUser(payload as AuthStatusResponse) : null;
}

export async function loginAsync(login: string, password: string) {
    const { payload } = await fetchAuthJson<AuthStatusResponse>(
        '/api/auth/login',
        {
            method: 'POST',
            body: JSON.stringify({
                login,
                password
            })
        },
        'Invalid username/email or password.'
    );

    return payload ? mapAuthUser(payload as AuthStatusResponse) : null;
}

export async function logoutAsync() {
    await fetchAuthJson(
        '/api/auth/logout',
        {
            method: 'POST'
        },
        'Unable to sign out right now.'
    );
}

export async function saveAccountAsync(draft: AccountDraft) {
    const { payload } = await fetchAuthJson<AuthStatusResponse>(
        '/api/auth/me',
        {
            method: 'PUT',
            body: JSON.stringify({
                userName: draft.username,
                email: draft.email.trim() || null,
                displayName: draft.displayName.trim() || null
            })
        },
        'Unable to save profile changes.'
    );

    return payload ? mapAuthUser(payload as AuthStatusResponse) : null;
}

export async function changePasswordAsync(currentPassword: string, newPassword: string) {
    await fetchAuthJson(
        '/api/auth/me/password',
        {
            method: 'POST',
            body: JSON.stringify({
                currentPassword,
                newPassword
            })
        },
        'Unable to change password right now.'
    );
}
