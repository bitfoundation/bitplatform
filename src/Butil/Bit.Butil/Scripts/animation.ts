var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _animations: { [id: string]: Animation } = {};

    // ScrollTimeline's own default source is the document's scrolling element, while CSS `scroll()`
    // defaults to the *nearest* scroll container - and the nearest one is what an animation inside a
    // scrolling panel means. So the walk up the tree is done here rather than left to the browser.
    function nearestScroller(animated: HTMLElement) {
        for (let node = animated?.parentElement; node; node = node.parentElement) {
            let overflow = '';
            try {
                const style = getComputedStyle(node);
                overflow = `${style.overflowX} ${style.overflowY}`;
            } catch {
                // Detached or in a document without a view - it cannot be a scrollport either way.
            }
            // `hidden` makes a scroll container too, even though the user cannot scroll it; `clip`
            // and `visible` do not.
            if (/\b(auto|scroll|overlay|hidden)\b/.test(overflow)) return node;
        }
        return document.scrollingElement;
    }

    // The times a scroll-driven animation reports are CSSNumericValue percentages of the range, not
    // milliseconds, and an idle animation reports none at all. Both travel with their unit so a 0%
    // progress is not confused with 0ms, and "no time yet" stays distinguishable from zero.
    function timeOf(value: any) {
        if (typeof value === 'number') return { value, unit: 'ms' };
        if (value && typeof value.value === 'number') return { value: value.value, unit: value.unit ?? '' };
        return null;
    }

    // A scroll-driven animation is progressed by a scroller's position (ScrollTimeline) or by how far
    // an element has moved through the scrollport (ViewTimeline) instead of by the clock. Duration
    // stops meaning milliseconds there, which is why `duration` is dropped when one is in play.
    function buildTimeline(timeline: any, animated: HTMLElement, source?: HTMLElement) {
        if (!timeline?.type) return null;

        const axis = timeline.axis || 'block';

        if (timeline.type === 'scroll') {
            const ST = (window as any).ScrollTimeline;
            if (typeof ST !== 'function') return null;
            // No source means the nearest scrollport, which is what `scroll()` defaults to in CSS.
            return new ST({ source: source ?? nearestScroller(animated), axis });
        }

        if (timeline.type === 'view') {
            const VT = (window as any).ViewTimeline;
            if (typeof VT !== 'function') return null;
            // The subject is the element whose passage through the scrollport drives the animation;
            // it defaults to the element being animated, which is the common case.
            return new VT({ subject: source ?? animated, axis });
        }

        return null;
    }

    butil.animation = {
        isTimelineSupported() { return typeof (window as any).ScrollTimeline === 'function'; },
        animate(id: string, element: HTMLElement, keyframes: Keyframe[], options: any, timelineSource?: HTMLElement) {
            if (!element || typeof element.animate !== 'function') return;
            // Map double.PositiveInfinity (sent as Infinity) → JS Infinity. JSON cannot represent
            // it natively, so dotnet sends "Infinity" as a string; normalize defensively.
            const iterations = options.iterations === 'Infinity' ? Infinity : options.iterations;
            const effect: any = {
                duration: options.duration,
                delay: options.delay,
                endDelay: options.endDelay,
                iterations,
                easing: options.easing,
                direction: options.direction,
                fill: options.fill,
                composite: options.composite
            };

            const timeline = buildTimeline(options.timeline, element, timelineSource);
            if (timeline) {
                effect.timeline = timeline;
                // On a progress-based timeline the animation spans the whole range unless a range is
                // named, and a millisecond duration would fight that.
                delete effect.duration;
                delete effect.delay;
                delete effect.endDelay;
                if (options.timeline.rangeStart) effect.rangeStart = options.timeline.rangeStart;
                if (options.timeline.rangeEnd) effect.rangeEnd = options.timeline.rangeEnd;
            }

            const animation = element.animate(keyframes, effect);
            _animations[id] = animation;
        },
        play(id: string) { _animations[id]?.play(); },
        pause(id: string) { _animations[id]?.pause(); },
        reverse(id: string) { _animations[id]?.reverse(); },
        cancel(id: string) {
            const a = _animations[id];
            if (!a) return;
            delete _animations[id];
            try { a.cancel(); } catch { /* already finished */ }
        },
        finish(id: string) {
            const a = _animations[id];
            if (!a) return;
            try { a.finish(); } catch { /* fillMode "none" rejects this */ }
        },
        async whenFinished(id: string) {
            const a = _animations[id];
            if (!a?.finished) return;
            try { await a.finished; } catch { /* canceled */ }
        },
        setPlaybackRate(id: string, rate: number) {
            const a = _animations[id];
            if (a) a.playbackRate = rate;
        },
        commitStyles(id: string) {
            const a: any = _animations[id];
            if (typeof a?.commitStyles !== 'function') return false;
            try {
                // Writes the animation's current computed values into the element's inline style, so
                // the state survives the animation being removed.
                a.commitStyles();
                return true;
            } catch {
                // Throws when the effect isn't in a state that can be committed (no target, or the
                // element isn't rendered).
                return false;
            }
        },
        persist(id: string) {
            const a: any = _animations[id];
            if (typeof a?.persist !== 'function') return false;
            try {
                // Opts the animation out of automatic removal: a filling animation that another one
                // supersedes is normally discarded, taking its visual effect with it.
                a.persist();
                return true;
            } catch {
                return false;
            }
        },
        getAnimations(element: HTMLElement, subtree: boolean) {
            // Every animation on the element, including CSS animations and transitions the page
            // never created through the Web Animations API - which is the point of asking.
            const source: any = element ?? document;
            if (typeof source.getAnimations !== 'function') return [];

            return source.getAnimations(element ? { subtree: !!subtree } : undefined).map((a: any) => {
                const current = timeOf(a.currentTime);
                const start = timeOf(a.startTime);

                return {
                    id: a.id ?? '',
                    playState: a.playState ?? '',
                    playbackRate: a.playbackRate ?? 1,
                    currentTime: current?.value ?? null,
                    currentTimeUnit: current?.unit ?? '',
                    startTime: start?.value ?? null,
                    startTimeUnit: start?.unit ?? '',
                    pending: a.pending === true,
                    replaceState: a.replaceState ?? '',
                    // The class name is the only portable way to tell a CSS animation, a CSS transition
                    // and a scripted animation apart.
                    kind: a.constructor?.name ?? 'Animation'
                };
            });
        },
        cancelAll(element: HTMLElement, subtree: boolean) {
            const source: any = element ?? document;
            if (typeof source.getAnimations !== 'function') return 0;

            const animations = source.getAnimations(element ? { subtree: !!subtree } : undefined);
            for (const a of animations) {
                try { a.cancel(); } catch { /* already finished */ }
            }
            return animations.length;
        }
    };
}(BitButil));
