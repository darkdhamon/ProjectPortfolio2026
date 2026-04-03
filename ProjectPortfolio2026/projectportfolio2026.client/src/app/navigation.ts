export interface NavigationOptions {
    replace?: boolean;
    preserveScroll?: boolean;
}

export type NavigateFn = (path: string, options?: NavigationOptions) => void;
