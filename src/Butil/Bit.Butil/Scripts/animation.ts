var BitButil = BitButil || {};

(function (butil: any) {
    const _animations: { [id: string]: Animation } = {};

    butil.animation = {
        animate(id: string, element: HTMLElement, keyframes: Keyframe[], options: any) {
            if (!element || typeof element.animate !== 'function') return;
            // Map double.PositiveInfinity (sent as Infinity) → JS Infinity. JSON cannot represent
            // it natively, so dotnet sends "Infinity" as a string; normalize defensively.
            const iterations = options.iterations === 'Infinity' ? Infinity : options.iterations;
            const animation = element.animate(keyframes, {
                duration: options.duration,
                delay: options.delay,
                endDelay: options.endDelay,
                iterations,
                easing: options.easing,
                direction: options.direction,
                fill: options.fill,
                composite: options.composite
            });
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
        }
    };
}(BitButil));
