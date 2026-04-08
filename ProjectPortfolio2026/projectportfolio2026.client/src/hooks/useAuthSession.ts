import { useCallback, useState } from 'react';
import { changePasswordAsync, fetchCurrentUser, loginAsync, logoutAsync, saveAccountAsync } from '../api/auth';
import { type AccountDraft, type AuthUser } from '../components/admin/mockAuth';

export function useAuthSession() {
    const [currentUser, setCurrentUser] = useState<AuthUser | null>(null);
    const [isAuthResolved, setIsAuthResolved] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(false);
    const [authError, setAuthError] = useState<string | null>(null);
    const [isSavingProfile, setIsSavingProfile] = useState(false);
    const [profileError, setProfileError] = useState<string | null>(null);
    const [profileNotice, setProfileNotice] = useState<string | null>(null);
    const [isSavingPassword, setIsSavingPassword] = useState(false);
    const [passwordError, setPasswordError] = useState<string | null>(null);
    const [passwordNotice, setPasswordNotice] = useState<string | null>(null);

    const refreshCurrentUser = useCallback(async (signal?: AbortSignal, reportErrors = true) => {
        try {
            const user = await fetchCurrentUser(signal);
            setCurrentUser(user);
            return user;
        } catch (caughtError) {
            if ((caughtError as Error).name === 'AbortError') {
                return null;
            }

            if (reportErrors) {
                setAuthError(caughtError instanceof Error ? caughtError.message : 'Unable to check the current session.');
            }

            setCurrentUser(null);
            return null;
        } finally {
            setIsAuthResolved(true);
        }
    }, []);

    const signIn = useCallback(async (login: string, password: string) => {
        setIsAuthenticating(true);
        setAuthError(null);

        try {
            const user = await loginAsync(login, password);
            setCurrentUser(user);
            setIsAuthResolved(true);
            return user;
        } catch (caughtError) {
            setAuthError(caughtError instanceof Error ? caughtError.message : 'Invalid username/email or password.');
            return null;
        } finally {
            setIsAuthenticating(false);
        }
    }, []);

    const signOut = useCallback(async () => {
        await logoutAsync();
        setCurrentUser(null);
        setProfileNotice(null);
        setPasswordNotice(null);
    }, []);

    const saveAccount = useCallback(async (draft: AccountDraft) => {
        setIsSavingProfile(true);
        setProfileError(null);
        setProfileNotice(null);

        try {
            const user = await saveAccountAsync(draft);
            setCurrentUser(user);
            setProfileNotice('Profile saved.');
        } catch (caughtError) {
            setProfileError(caughtError instanceof Error ? caughtError.message : 'Unable to save profile changes.');
        } finally {
            setIsSavingProfile(false);
        }
    }, []);

    const changePassword = useCallback(async (currentPassword: string, newPassword: string, confirmPassword: string) => {
        setIsSavingPassword(true);
        setPasswordError(null);
        setPasswordNotice(null);

        try {
            if (newPassword !== confirmPassword) {
                throw new Error('New password and confirmation must match.');
            }

            await changePasswordAsync(currentPassword, newPassword);
            setPasswordNotice('Password updated.');
        } catch (caughtError) {
            setPasswordError(caughtError instanceof Error ? caughtError.message : 'Unable to change password right now.');
        } finally {
            setIsSavingPassword(false);
        }
    }, []);

    return {
        authError,
        changePassword,
        currentUser,
        isAuthResolved,
        isAuthenticating,
        isSavingPassword,
        isSavingProfile,
        passwordError,
        passwordNotice,
        profileError,
        profileNotice,
        refreshCurrentUser,
        saveAccount,
        setAuthError,
        signIn,
        signOut
    };
}
