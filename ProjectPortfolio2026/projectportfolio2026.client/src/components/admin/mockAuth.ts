export interface MockAuthUser {
    username: string;
    email: string;
    displayName: string;
    roles: string[];
}

export interface AccountDraft {
    username: string;
    email: string;
    displayName: string;
}

export const mockAdminUser: MockAuthUser = {
    username: 'admin',
    email: 'admin@example.com',
    displayName: '',
    roles: ['Admin']
};

export const isAuthMockupPreviewEnabled = true;
