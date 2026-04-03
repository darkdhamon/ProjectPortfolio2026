import { useEffect, useRef, useState, type KeyboardEvent, type SyntheticEvent, type TouchEvent } from 'react';
import screenshotMissing from '../../assets/Placeholders/Screenshot-Missing.png';
import type { ProjectScreenshot } from '../../app/types';
import { MediaFrame } from '../../components/common/MediaFrame';
import { useMediaQuery } from '../../hooks/useMediaQuery';

interface ScreenshotCarouselProps {
    projectTitle: string;
    screenshots: ProjectScreenshot[];
}

export function ScreenshotCarousel({
    projectTitle,
    screenshots
}: ScreenshotCarouselProps) {
    const [activeIndex, setActiveIndex] = useState(0);
    const isMobile = useMediaQuery('(max-width: 720px)');
    const [transitionDirection, setTransitionDirection] = useState<'prev' | 'next'>('next');
    const [fullscreenIndex, setFullscreenIndex] = useState<number | null>(null);
    const [screenshotRatios, setScreenshotRatios] = useState<Record<number, number>>({});
    const touchStartXRef = useRef<number | null>(null);

    const activeScreenshotIndex = activeIndex >= screenshots.length ? 0 : activeIndex;

    useEffect(() => {
        if (fullscreenIndex === null) {
            return;
        }

        const handleKeyDown = (event: globalThis.KeyboardEvent) => {
            if (event.key === 'Escape') {
                setFullscreenIndex(null);
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [fullscreenIndex]);

    const displayScreenshots = screenshots.length === 1
        ? [{
            screenshot: screenshots[0],
            state: 'active',
            key: `active-${screenshots[0]?.sortOrder ?? 0}`,
            index: 0
        }]
        : screenshots.length === 2
            ? screenshots.flatMap((screenshot, index) => {
                const isActive = index === activeScreenshotIndex;

                return [
                    {
                        screenshot,
                        state: isActive ? 'active' : transitionDirection === 'next' ? 'next1' : 'prev1',
                        key: `center-${screenshot.sortOrder}-${index}`,
                        index
                    },
                    {
                        screenshot,
                        state: isActive ? 'hidden' : transitionDirection === 'next' ? 'prev1' : 'next1',
                        key: `side-${screenshot.sortOrder}-${index}`,
                        index
                    }
                ];
            })
            : screenshots.map((screenshot, index) => ({
                screenshot,
                state: getCarouselCardState(index, activeScreenshotIndex, screenshots.length),
                key: `${screenshot.sortOrder}-${screenshot.imageUrl}`,
                index
            }));

    function showRelativeScreenshot(step: number) {
        setTransitionDirection(step < 0 ? 'prev' : 'next');
        setActiveIndex(currentIndex => {
            const nextIndex = currentIndex + step;
            return (nextIndex + screenshots.length) % screenshots.length;
        });
    }

    function showPreviousScreenshot() {
        if (screenshots.length <= 1) {
            return;
        }

        showRelativeScreenshot(-1);
    }

    function showNextScreenshot() {
        if (screenshots.length <= 1) {
            return;
        }

        showRelativeScreenshot(1);
    }

    function showScreenshot(nextIndex: number) {
        if (nextIndex !== activeScreenshotIndex) {
            setActiveIndex(nextIndex);
        }
    }

    function openFullscreenScreenshot(nextIndex: number) {
        if (nextIndex !== activeScreenshotIndex) {
            setTransitionDirection(nextIndex < activeScreenshotIndex ? 'prev' : 'next');
            setActiveIndex(nextIndex);
        }

        setFullscreenIndex(nextIndex);
    }

    function handleScreenshotLoad(index: number, event: SyntheticEvent<HTMLImageElement>) {
        const { naturalWidth, naturalHeight } = event.currentTarget;
        if (naturalWidth <= 0 || naturalHeight <= 0) {
            return;
        }

        const ratio = naturalWidth / naturalHeight;
        setScreenshotRatios(currentRatios => {
            if (currentRatios[index] === ratio) {
                return currentRatios;
            }

            return {
                ...currentRatios,
                [index]: ratio
            };
        });
    }

    function handleScreenshotCarouselKeyDown(event: KeyboardEvent<HTMLElement>) {
        if (isMobile || screenshots.length <= 1) {
            return;
        }

        if (event.key === 'ArrowLeft') {
            event.preventDefault();
            showPreviousScreenshot();
        }

        if (event.key === 'ArrowRight') {
            event.preventDefault();
            showNextScreenshot();
        }
    }

    function handleTouchStart(event: TouchEvent<HTMLElement>) {
        touchStartXRef.current = event.changedTouches[0]?.clientX ?? null;
    }

    function handleTouchEnd(event: TouchEvent<HTMLElement>) {
        const touchStartX = touchStartXRef.current;
        const touchEndX = event.changedTouches[0]?.clientX ?? null;

        touchStartXRef.current = null;

        if (touchStartX === null || touchEndX === null) {
            return;
        }

        const swipeDelta = touchStartX - touchEndX;
        if (Math.abs(swipeDelta) < 40 || screenshots.length <= 1) {
            return;
        }

        if (swipeDelta > 0) {
            showNextScreenshot();
            return;
        }

        showPreviousScreenshot();
    }

    const activeScreenshot = screenshots[activeScreenshotIndex] ?? null;
    const fullscreenScreenshot = fullscreenIndex === null ? null : screenshots[fullscreenIndex];
    const activeAspectRatio = screenshotRatios[activeScreenshotIndex] ?? (16 / 9);

    return (
        <>
            <div className="detail-screenshot-block">
                <section
                    className={`detail-carousel-shell${isMobile ? ' mobile' : ''}`}
                    aria-label="Project screenshot carousel"
                    aria-roledescription="carousel"
                    tabIndex={0}
                    style={{ aspectRatio: `${activeAspectRatio}` }}
                    onKeyDown={handleScreenshotCarouselKeyDown}
                    onTouchStart={handleTouchStart}
                    onTouchEnd={handleTouchEnd}>
                    <div className="detail-carousel-track">
                        {displayScreenshots.map(({ screenshot, state, key, index }) => (
                            <figure
                                key={key}
                                aria-hidden={state === 'hidden'}
                                style={{ aspectRatio: `${screenshotRatios[index] ?? activeAspectRatio}` }}
                                className={`detail-carousel-slide ${state}`}>
                                <button
                                    className="detail-carousel-trigger"
                                    type="button"
                                    onClick={() => openFullscreenScreenshot(index)}
                                    aria-label={`Open screenshot ${index + 1} in fullscreen`}>
                                    <MediaFrame
                                        src={screenshot.imageUrl}
                                        alt={screenshot.caption?.trim() || `${projectTitle} screenshot ${screenshot.sortOrder}`}
                                        fallbackLabel={screenshot.caption?.trim() || `${projectTitle} ${screenshot.sortOrder}`}
                                        fallbackSrc={screenshotMissing}
                                        className="detail-carousel-media"
                                        onLoad={event => handleScreenshotLoad(index, event)}
                                    />
                                </button>
                            </figure>
                        ))}
                    </div>

                    {!isMobile && screenshots.length > 1 ? (
                        <>
                            <button
                                className="carousel-arrow carousel-arrow-left"
                                type="button"
                                onClick={showPreviousScreenshot}
                                aria-label="Show previous screenshot">
                                {'<'}
                            </button>
                            <button
                                className="carousel-arrow carousel-arrow-right"
                                type="button"
                                onClick={showNextScreenshot}
                                aria-label="Show next screenshot">
                                {'>'}
                            </button>
                        </>
                    ) : null}
                </section>

                {screenshots.length > 1 ? (
                    <div className="detail-carousel-indicators" aria-label="Project screenshot selection">
                        {screenshots.map((screenshot, index) => (
                            <button
                                key={`${screenshot.sortOrder}-${screenshot.imageUrl}-indicator`}
                                className={`carousel-indicator${index === activeScreenshotIndex ? ' active' : ''}`}
                                type="button"
                                onClick={() => showScreenshot(index)}
                                aria-label={`Show screenshot ${index + 1}`}
                                aria-pressed={index === activeScreenshotIndex}
                            />
                        ))}
                    </div>
                ) : null}

                {activeScreenshot ? (
                    <div className="detail-carousel-caption">
                        <p className="eyebrow">Screenshot {activeScreenshotIndex + 1} of {screenshots.length}</p>
                        <p>{activeScreenshot.caption?.trim() || `${projectTitle} interface preview.`}</p>
                    </div>
                ) : null}
            </div>
            {fullscreenScreenshot ? (
                <div className="lightbox-backdrop" role="dialog" aria-modal="true" aria-label="Fullscreen screenshot viewer">
                    <div className="lightbox-shell">
                        <button
                            className="lightbox-close"
                            type="button"
                            onClick={() => setFullscreenIndex(null)}
                            aria-label="Close image">
                            Close
                        </button>
                        <MediaFrame
                            src={fullscreenScreenshot.imageUrl}
                            alt={fullscreenScreenshot.caption?.trim() || `${projectTitle} fullscreen screenshot ${fullscreenScreenshot.sortOrder}`}
                            fallbackLabel={fullscreenScreenshot.caption?.trim() || `${projectTitle} ${fullscreenScreenshot.sortOrder}`}
                            fallbackSrc={screenshotMissing}
                            className="lightbox-image"
                        />
                        <p className="lightbox-caption">
                            {fullscreenScreenshot.caption?.trim() || `${projectTitle} interface preview.`}
                        </p>
                    </div>
                </div>
            ) : null}
        </>
    );
}

function getCarouselCardState(index: number, activeIndex: number, total: number) {
    if (index === activeIndex) {
        return 'active';
    }

    if (index === (activeIndex - 1 + total) % total) {
        return 'prev1';
    }

    if (index === (activeIndex - 2 + total) % total) {
        return 'prev2';
    }

    if (index === (activeIndex + 1) % total) {
        return 'next1';
    }

    if (index === (activeIndex + 2) % total) {
        return 'next2';
    }

    return 'hidden';
}
