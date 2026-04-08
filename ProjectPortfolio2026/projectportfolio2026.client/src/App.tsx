import { useEffect, useMemo, useState } from 'react';
import type { NavigateFn } from './app/navigation';
import { parseRoute, readLocation, type AppLocation } from './appSupport';
import { AccountSettingsPage } from './components/admin/AccountSettingsPage';
import { AdminDashboardPage } from './components/admin/AdminDashboardPage';
import { LoginPage } from './components/admin/LoginPage';
import { type AccountDraft } from './components/admin/mockAuth';
import { SiteShell, type SiteShellContent } from './components/shell/SiteShell';
import { HomePage } from './features/home/HomePage';
import { ProjectDetailPage } from './features/projects/ProjectDetailPage';
import { ProjectListPage } from './features/projects/ProjectListPage';
import { useAuthSession } from './hooks/useAuthSession';
import './App.css';

function App() {
    const [location, setLocation] = useState<AppLocation>(() => readLocation());
    const {
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
    } = useAuthSession();

    useEffect(() => {
        const handlePopState = () => {
            setLocation(readLocation());
        };

        window.addEventListener('popstate', handlePopState);
        return () => window.removeEventListener('popstate', handlePopState);
    }, []);

    const route = useMemo(() => parseRoute(location), [location]);
    const isAdminRoute = route.kind === 'admin' || route.kind === 'admin-account';
    const activeNavLabel = route.kind === 'home'
        ? 'Home'
        : route.kind === 'detail' || route.kind === 'list'
            ? 'Projects'
            : route.kind === 'login' || route.kind === 'admin' || route.kind === 'admin-account'
                ? 'Admin'
                : '';
    const displayName = currentUser?.displayName.trim() ? currentUser.displayName.trim() : currentUser?.username ?? '';
    const shellContent = route.kind === 'home'
        ? {
            kicker: 'Project Portfolio',
            title: 'Home',
            summary: 'Start with a short introduction, then move through featured work before exploring the broader project archive.'
        } satisfies SiteShellContent
        : route.kind === 'detail'
            ? {
                kicker: 'Project Portfolio',
                title: 'Project Detail',
                summary: 'Review the project story, supporting media, collaborators, and milestone context.'
            } satisfies SiteShellContent
            : route.kind === 'login'
                ? {
                    kicker: 'Admin Access',
                    title: 'Log In',
                    summary: 'Use the admin entry point to sign in, review the protected dashboard, and manage your own account settings.'
                } satisfies SiteShellContent
                : route.kind === 'admin'
                    ? {
                        kicker: 'Admin Access',
                        title: 'Admin Dashboard',
                        summary: 'This placeholder dashboard confirms the protected admin flow while the rest of the management surface is still being built.'
                    } satisfies SiteShellContent
                    : route.kind === 'admin-account'
                        ? {
                            kicker: 'Admin Access',
                            title: 'Account Settings',
                            summary: 'Update your username, email, display name, and password through the authenticated admin account flow.'
                        } satisfies SiteShellContent
                        : {
                            kicker: 'Project Portfolio',
                            title: 'Projects',
                            summary: 'Browse shipped work, search the portfolio, and move into individual project case studies.'
                        } satisfies SiteShellContent;

    const navigate: NavigateFn = (nextPath, options) => {
        const method = options?.replace ? 'replaceState' : 'pushState';
        window.history[method](window.history.state, '', nextPath);
        setLocation(readLocation());

        if (!options?.preserveScroll) {
            window.scrollTo({ top: 0, behavior: 'auto' });
        }
    };

    useEffect(() => {
        const controller = new AbortController();
        const shouldReportSessionErrors = isAdminRoute || route.kind === 'login';
        setAuthError(null);
        void refreshCurrentUser(controller.signal, shouldReportSessionErrors);

        return () => controller.abort();
    }, [isAdminRoute, route.kind, location.pathname, location.search, refreshCurrentUser, setAuthError]);

    useEffect(() => {
        if (!isAdminRoute || !isAuthResolved) {
            return;
        }

        if (!currentUser?.isAdmin) {
            const redirectTarget = `${location.pathname}${location.search}` || '/admin';
            const redirectTimeoutId = window.setTimeout(() => {
                navigate(`/login?redirect=${encodeURIComponent(redirectTarget)}`, { replace: true });
            }, 0);

            return () => window.clearTimeout(redirectTimeoutId);
        }
    }, [currentUser, isAdminRoute, isAuthResolved, location.pathname, location.search]);

    async function handleAdminNavigate() {
        const user = await refreshCurrentUser();
        if (user?.isAdmin) {
            navigate('/admin');
            return;
        }

        navigate('/login?redirect=%2Fadmin');
    }

    async function handleLogin(login: string, password: string, redirectTo: string) {
        const user = await signIn(login, password);
        if (user) {
            navigate(redirectTo);
        }
    }

    async function handleLogout() {
        await signOut();
        navigate('/login?redirect=%2Fadmin');
    }

    async function handleAccountSave(draft: AccountDraft) {
        await saveAccount(draft);
    }

    async function handlePasswordChange(currentPassword: string, newPassword: string, confirmPassword: string) {
        await changePassword(currentPassword, newPassword, confirmPassword);
    }

    const isAdminAuthenticated = currentUser?.isAdmin === true;

    return (
        <SiteShell
            activeNavLabel={activeNavLabel}
            content={shellContent}
            currentUserDisplayName={displayName}
            isAuthenticated={isAdminAuthenticated}
            onAdminNavigate={handleAdminNavigate}
            onLogout={handleLogout}
            onNavigate={navigate}>
            {route.kind === 'home' ? (
                <HomePage onNavigate={navigate} />
            ) : route.kind === 'detail' ? (
                <ProjectDetailPage
                    projectId={route.projectId}
                    listSearch={route.listSearch}
                    onNavigate={navigate} />
            ) : route.kind === 'login' ? (
                <LoginPage
                    redirectTo={route.redirectTo}
                    isSubmitting={isAuthenticating}
                    errorMessage={authError}
                    onSignIn={handleLogin}
                />
            ) : route.kind === 'admin' && isAdminAuthenticated ? (
                <AdminDashboardPage
                    currentUserDisplayName={displayName}
                    onNavigate={navigate}
                />
            ) : route.kind === 'admin-account' && currentUser ? (
                <AccountSettingsPage
                    currentUser={currentUser}
                    isSavingProfile={isSavingProfile}
                    profileError={profileError}
                    profileNotice={profileNotice}
                    isSavingPassword={isSavingPassword}
                    passwordError={passwordError}
                    passwordNotice={passwordNotice}
                    onSave={handleAccountSave}
                    onChangePassword={handlePasswordChange}
                />
            ) : isAdminRoute ? (
                <main className="auth-page">
                    <section className="auth-card">
                        <div className="auth-intro">
                            <p className="eyebrow">Admin Access</p>
                            <h1>Checking your session.</h1>
                            <p className="hero-description">
                                {authError ?? 'Verifying whether you are already signed in before opening the admin area.'}
                            </p>
                        </div>
                    </section>
                </main>
            ) : (
                <ProjectListPage
                    filters={route.filters}
                    onNavigate={navigate} />
            )}
        </SiteShell>
    );
}

export default App;
