import { useEffect, useMemo, useState } from 'react';
import type { NavigateFn } from './app/navigation';
import { parseRoute, readLocation, type AppLocation } from './appSupport';
import { SiteShell, type SiteShellContent } from './components/shell/SiteShell';
import { HomePage } from './features/home/HomePage';
import { ProjectDetailPage } from './features/projects/ProjectDetailPage';
import { ProjectListPage } from './features/projects/ProjectListPage';
import './App.css';

function App() {
    const [location, setLocation] = useState<AppLocation>(() => readLocation());

    useEffect(() => {
        const handlePopState = () => {
            setLocation(readLocation());
        };

        window.addEventListener('popstate', handlePopState);
        return () => window.removeEventListener('popstate', handlePopState);
    }, []);

    const route = useMemo(() => parseRoute(location), [location]);
    const activeNavLabel = route.kind === 'home'
        ? 'Home'
        : route.kind === 'detail' || route.kind === 'list'
            ? 'Projects'
            : '';
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

    return (
        <SiteShell
            activeNavLabel={activeNavLabel}
            content={shellContent}
            onNavigate={navigate}>
            {route.kind === 'home' ? (
                <HomePage onNavigate={navigate} />
            ) : route.kind === 'detail' ? (
                <ProjectDetailPage
                    projectId={route.projectId}
                    listSearch={route.listSearch}
                    onNavigate={navigate} />
            ) : (
                <ProjectListPage
                    filters={route.filters}
                    onNavigate={navigate} />
            )}
        </SiteShell>
    );
}

export default App;
