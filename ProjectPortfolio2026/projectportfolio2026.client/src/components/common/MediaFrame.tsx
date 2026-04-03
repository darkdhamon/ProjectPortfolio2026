import { useState, type SyntheticEvent } from 'react';

interface MediaFrameProps {
    src?: string | null;
    alt: string;
    fallbackLabel: string;
    fallbackSrc?: string;
    className?: string;
    compact?: boolean;
    onLoad?: (event: SyntheticEvent<HTMLImageElement>) => void;
}

export function MediaFrame({
    src,
    alt,
    fallbackLabel,
    fallbackSrc,
    className,
    compact = false,
    onLoad
}: MediaFrameProps) {
    const [failedSource, setFailedSource] = useState<string | null>(null);
    const hasFailedPrimary = !!src && failedSource === src;
    const hasFailedFallback = !!fallbackSrc && failedSource === fallbackSrc;
    const candidateSrc = (!src || hasFailedPrimary)
        ? (hasFailedFallback ? undefined : fallbackSrc)
        : src;

    if (!candidateSrc) {
        return (
            <div className={`${className ?? ''} media-fallback${compact ? ' compact' : ''}`.trim()}>
                <span>{fallbackLabel.slice(0, compact ? 2 : 3).toUpperCase()}</span>
            </div>
        );
    }

    return (
        <img
            className={className}
            src={candidateSrc}
            alt={alt}
            loading="lazy"
            onLoad={onLoad}
            onError={() => setFailedSource(candidateSrc)}
        />
    );
}
