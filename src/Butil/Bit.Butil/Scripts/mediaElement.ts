var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Everything here operates on an <audio>/<video> the caller already has an ElementReference to.
    // Nothing is cached: the element is the state, and reading it back is a property access.
    function media(element: any): HTMLMediaElement | null {
        return element && typeof element.play === 'function' ? element as HTMLMediaElement : null;
    }

    butil.mediaElement = {
        async play(element: any) {
            const el = media(element);
            if (!el) return false;
            try {
                await el.play();
                return true;
            } catch {
                // NotAllowedError when autoplay is blocked and there was no user gesture, or
                // NotSupportedError when the source can't be decoded. Both mean "didn't play".
                return false;
            }
        },
        pause(element: any) { media(element)?.pause(); },
        load(element: any) { media(element)?.load(); },
        getState(element: any) {
            const el = media(element);
            if (!el) return null;
            return {
                paused: el.paused,
                ended: el.ended,
                seeking: el.seeking,
                muted: el.muted,
                loop: el.loop,
                autoplay: el.autoplay,
                volume: el.volume,
                playbackRate: el.playbackRate,
                currentTime: el.currentTime,
                // NaN before metadata loads, and Infinity for a live stream - neither survives
                // JSON, so both become 0 and the caller checks readyState instead.
                duration: Number.isFinite(el.duration) ? el.duration : 0,
                readyState: el.readyState,
                networkState: el.networkState,
                currentSrc: el.currentSrc ?? '',
                bufferedEnd: el.buffered.length ? el.buffered.end(el.buffered.length - 1) : 0,
                videoWidth: (el as any).videoWidth ?? 0,
                videoHeight: (el as any).videoHeight ?? 0
            };
        },
        setCurrentTime(element: any, seconds: number) {
            const el = media(element);
            if (!el) return false;
            try { el.currentTime = seconds; return true; }
            catch { return false; }   // seeking before the media is seekable throws
        },
        setVolume(element: any, volume: number) {
            const el = media(element);
            if (!el) return;
            // Out of range throws IndexSizeError, so clamp rather than propagate.
            el.volume = Math.max(0, Math.min(1, volume));
        },
        setMuted(element: any, muted: boolean) { const el = media(element); if (el) el.muted = muted; },
        setLoop(element: any, loop: boolean) { const el = media(element); if (el) el.loop = loop; },
        setPlaybackRate(element: any, rate: number) {
            const el = media(element);
            if (!el) return false;
            try { el.playbackRate = rate; return true; }
            catch { return false; }   // engines reject rates outside what they can resample
        },
        setSrc(element: any, src: string) {
            const el = media(element);
            if (!el) return;
            el.src = src;
        },
        canPlayType(element: any, mimeType: string) {
            const el = media(element);
            // "probably" | "maybe" | "" - the browser's own three-valued answer, kept as-is.
            return el?.canPlayType(mimeType) ?? '';
        }
    };
}(BitButil));
