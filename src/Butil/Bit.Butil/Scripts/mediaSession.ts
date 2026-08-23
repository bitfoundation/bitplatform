var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The set of actions we have installed a handler for, so unsetting can clear exactly those and
    // nothing a different page (or a previous circuit) registered.
    const _actions = new Set<string>();

    function session() { return (window.navigator as any).mediaSession; }

    butil.mediaSession = {
        isSupported() { return !!session(); },
        setMetadata(metadata: any) {
            const s = session();
            const M = (window as any).MediaMetadata;
            if (!s) return false;
            if (!metadata) { s.metadata = null; return true; }
            if (typeof M !== 'function') return false;
            try {
                s.metadata = new M({
                    title: metadata.title ?? '',
                    artist: metadata.artist ?? '',
                    album: metadata.album ?? '',
                    // Several sizes let the platform pick what fits: a lock screen wants a large
                    // one, a system tray a small one.
                    artwork: (metadata.artwork ?? []).map((a: any) => ({
                        src: a.src,
                        sizes: a.sizes ?? undefined,
                        type: a.type ?? undefined
                    }))
                });
                return true;
            } catch {
                // A malformed artwork URL rejects the whole MediaMetadata construction.
                return false;
            }
        },
        setPlaybackState(state: string) {
            const s = session();
            if (!s) return;
            // 'none' | 'paused' | 'playing'; anything else would throw.
            try { s.playbackState = state; } catch { /* unknown state on this engine */ }
        },
        setPositionState(duration: number, playbackRate: number, position: number) {
            const s = session();
            if (!s?.setPositionState) return false;
            try {
                // Clamped because the spec throws when position > duration, which is easy to hit
                // when a media element's currentTime overshoots its reported duration slightly.
                s.setPositionState({
                    duration,
                    playbackRate: playbackRate || 1,
                    position: Math.max(0, Math.min(position, duration))
                });
                return true;
            } catch {
                return false;
            }
        },
        clearPositionState() {
            const s = session();
            if (!s?.setPositionState) return;
            try { s.setPositionState(); } catch { /* nothing to clear */ }
        },
        setActionHandler(dotNetRef: any, action: string) {
            const s = session();
            if (!s?.setActionHandler) return false;
            try {
                s.setActionHandler(action, (details: any) => butil.utils.dispatch(dotNetRef, 'InvokeMediaSessionAction', action, {
                    action,
                    seekTime: details?.seekTime ?? null,
                    seekOffset: details?.seekOffset ?? null,
                    fastSeek: !!details?.fastSeek
                }));
                _actions.add(action);
                return true;
            } catch {
                // Engines throw TypeError for actions they don't implement (e.g. 'seekto' on older
                // builds), which is exactly the "unsupported" signal the caller wants.
                return false;
            }
        },
        clearActionHandler(action: string) {
            const s = session();
            if (!s?.setActionHandler) return;
            _actions.delete(action);
            try { s.setActionHandler(action, null); } catch { /* never supported here */ }
        },
        disposeAll() {
            const s = session();
            if (!s) return;
            for (const action of Array.from(_actions)) {
                try { s.setActionHandler(action, null); } catch { /* never supported here */ }
            }
            _actions.clear();
            try { s.metadata = null; } catch { /* ignore */ }
            try { s.playbackState = 'none'; } catch { /* ignore */ }
        }
    };
}(BitButil));
