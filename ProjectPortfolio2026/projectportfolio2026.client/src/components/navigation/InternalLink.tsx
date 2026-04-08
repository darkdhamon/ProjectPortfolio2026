export type NavigateOptions = {
    replace?: boolean;
    preserveScroll?: boolean;
};

export type NavigateHandler = (path: string, options?: NavigateOptions) => void;

export function InternalLink({
    className,
    href,
    ariaCurrent,
    onNavigate,
    children,
    preserveScroll
}: {
    className?: string;
    href: string;
    ariaCurrent?: 'page';
    onNavigate: NavigateHandler;
    children: string;
    preserveScroll?: boolean;
}) {
    return (
        <a
            className={className}
            href={href}
            aria-current={ariaCurrent}
            onClick={event => {
                event.preventDefault();
                onNavigate(href, { preserveScroll });
            }}>
            {children}
        </a>
    );
}
