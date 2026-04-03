import type { ReactNode } from 'react';
import type { NavigateFn } from '../../app/navigation';

interface InternalLinkProps {
    className?: string;
    href: string;
    ariaCurrent?: 'page';
    onNavigate: NavigateFn;
    children: ReactNode;
    preserveScroll?: boolean;
}

export function InternalLink({
    className,
    href,
    ariaCurrent,
    onNavigate,
    children,
    preserveScroll
}: InternalLinkProps) {
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
