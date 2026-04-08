export interface ListFilters {
    searchInput: string;
    selectedSkills: string[];
}

export interface AppLocation {
    pathname: string;
    search: string;
}

const homeRoutePattern = /^\/?$/;
const loginRoutePattern = /^\/login\/?$/;
const adminRoutePattern = /^\/admin\/?$/;
const adminAccountRoutePattern = /^\/admin\/account\/?$/;
const listRoutePattern = /^\/projects\/?$/;
const detailRoutePattern = /^\/projects\/(?<id>\d+)\/?$/;
const contactRoutePattern = /^\/contact\/?$/;

export function parseRoute(location: AppLocation) {
    if (homeRoutePattern.test(location.pathname)) {
        return {
            kind: 'home' as const
        };
    }

    if (loginRoutePattern.test(location.pathname)) {
        return {
            kind: 'login' as const,
            redirectTo: parseRedirectTarget(location.search)
        };
    }

    if (adminAccountRoutePattern.test(location.pathname)) {
        return {
            kind: 'admin-account' as const
        };
    }

    if (adminRoutePattern.test(location.pathname)) {
        return {
            kind: 'admin' as const
        };
    }

    const match = detailRoutePattern.exec(location.pathname);
    if (match?.groups?.id) {
        return {
            kind: 'detail' as const,
            projectId: Number.parseInt(match.groups.id, 10),
            listSearch: location.search
        };
    }

    if (listRoutePattern.test(location.pathname)) {
        return {
            kind: 'list' as const,
            filters: parseListFilters(location.search)
        };
    }

    if (contactRoutePattern.test(location.pathname)) {
        return {
            kind: 'contact' as const
        };
    }

    return {
        kind: 'home' as const
    };
}

export function parseRedirectTarget(search: string) {
    const params = new URLSearchParams(search);
    const redirect = params.get('redirect');

    if (!redirect || !redirect.startsWith('/')) {
        return '/admin';
    }

    return redirect;
}

export function parseListFilters(search: string): ListFilters {
    const params = new URLSearchParams(search);
    return {
        searchInput: params.get('search')?.trim() ?? '',
        selectedSkills: parseSkills(params.get('skills'))
    };
}

export function buildListSearch(filters: ListFilters) {
    const params = new URLSearchParams();
    const trimmedSearch = filters.searchInput.trim();

    if (trimmedSearch.length > 0) {
        params.set('search', trimmedSearch);
    }

    if (filters.selectedSkills.length > 0) {
        params.set('skills', filters.selectedSkills.join(','));
    }

    const search = params.toString();
    return search.length > 0 ? `?${search}` : '';
}

export function buildProjectsPath(listSearch: string) {
    return `/projects${listSearch}`;
}

export function buildDetailPath(projectId: number, listSearch: string) {
    return `/projects/${projectId}${listSearch}`;
}

export function createRouteKey(filters: ListFilters) {
    return JSON.stringify({
        searchInput: filters.searchInput.trim(),
        selectedSkills: [...filters.selectedSkills].sort((left, right) => left.localeCompare(right))
    });
}

export function parseSkills(skills: string | null) {
    if (!skills) {
        return [];
    }

    return skills
        .split(',')
        .map(skill => skill.trim())
        .filter((skill, index, allSkills) => skill.length > 0 && allSkills.indexOf(skill) === index);
}

export function renderMarkdownParagraphs(markdown: string) {
    return markdown
        .split(/\r?\n\r?\n/)
        .map(paragraph => paragraph.trim())
        .filter(paragraph => paragraph.length > 0)
        .map(paragraph => <p key={paragraph}>{paragraph.replace(/^#+\s*/, '')}</p>);
}

export function mergeProjects<TProject extends { id: number }>(currentProjects: TProject[], nextProjects: TProject[]) {
    const seen = new Set(currentProjects.map(project => project.id));
    const mergedProjects = [...currentProjects];

    for (const project of nextProjects) {
        if (!seen.has(project.id)) {
            mergedProjects.push(project);
            seen.add(project.id);
        }
    }

    return mergedProjects;
}

export function formatProjectDates(startDate: string, endDate?: string | null) {
    const start = formatMonth(startDate);
    const end = endDate ? formatMonth(endDate) : 'Present';
    return `${start} to ${end}`;
}

export function getProjectYearSpacerLabel(endDate?: string | null) {
    if (!endDate) {
        return 'Present';
    }

    return new Date(`${endDate}T00:00:00`).getFullYear().toString();
}

export function formatMonth(value: string) {
    return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
        month: 'short',
        year: 'numeric'
    });
}

export function formatFullDate(value: string) {
    return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    });
}

export function readLocation(): AppLocation {
    return {
        pathname: window.location.pathname,
        search: window.location.search
    };
}
