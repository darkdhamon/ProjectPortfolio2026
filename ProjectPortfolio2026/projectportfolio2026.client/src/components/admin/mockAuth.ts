export interface AuthUser {
    username: string;
    email: string | null;
    displayName: string;
    isAdmin: boolean;
}

export interface AccountDraft {
    username: string;
    email: string;
    displayName: string;
}
