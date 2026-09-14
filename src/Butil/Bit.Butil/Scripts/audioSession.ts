var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: any } = {};

    function session() { return (window.navigator as any).audioSession; }

    butil.audioSession = {
        isSupported() { return !!session(); },
        getType() { return session()?.type ?? ''; },
        setType(type: string) {
            const audioSession = session();
            if (!audioSession) return false;
            try {
                // Declaring the intent is what lets the OS decide correctly: whether to duck other
                // audio, mix with it, or take it over - and whether the sound follows the ringer
                // switch on a phone.
                audioSession.type = type;
                return true;
            } catch {
                return false;
            }
        },
        getState() { return session()?.state ?? ''; },
        onStateChange(dotNetRef: any, listenerId: string, method: string) {
            const audioSession = session();
            if (!audioSession?.addEventListener) return false;

            const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, audioSession.state ?? '');
            audioSession.addEventListener('statechange', handler);
            _listeners[listenerId] = handler;

            butil.utils.dispatch(dotNetRef, method, listenerId, audioSession.state ?? '');
            return true;
        },
        offStateChange(listenerId: string) {
            const handler = _listeners[listenerId];
            if (!handler) return;
            delete _listeners[listenerId];
            try { session()?.removeEventListener('statechange', handler); } catch { /* session gone */ }
        }
    };
}(BitButil));
