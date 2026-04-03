import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

class MockIntersectionObserver implements IntersectionObserver {
    readonly root = null;
    readonly rootMargin = '0px';
    readonly thresholds = [0];

    disconnect() {
        return undefined;
    }

    observe() {
        return undefined;
    }

    takeRecords(): IntersectionObserverEntry[] {
        return [];
    }

    unobserve() {
        return undefined;
    }
}

if (typeof window !== 'undefined') {
    Object.defineProperty(window, 'IntersectionObserver', {
        writable: true,
        configurable: true,
        value: MockIntersectionObserver
    });

    Object.defineProperty(window, 'scrollTo', {
        writable: true,
        configurable: true,
        value: vi.fn()
    });
}
