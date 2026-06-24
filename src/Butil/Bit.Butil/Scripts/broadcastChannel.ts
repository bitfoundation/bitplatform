var BitButil = BitButil || {};

(function (butil: any) {
    // Per-channel reference counting so multiple subscribers share one BroadcastChannel instance.
    const _channels: { [name: string]: { ch: BroadcastChannel, subscribers: number } } = {};
    const _subscribers: { [id: string]: { name: string, onMessage: (e: MessageEvent) => void, onError: () => void } } = {};

    butil.broadcastChannel = {
        isSupported() { return 'BroadcastChannel' in window; },
        post,
        subscribe,
        unsubscribe
    };

    function getChannel(name: string) {
        let entry = _channels[name];
        if (!entry) {
            entry = { ch: new BroadcastChannel(name), subscribers: 0 };
            _channels[name] = entry;
        }
        return entry;
    }

    function post(channelName: string, message: any) {
        if (!('BroadcastChannel' in window)) return;
        // Use a transient channel for fire-and-forget posts so we never accumulate stray channels
        // when nobody is subscribing in this tab.
        const entry = _channels[channelName];
        if (entry) {
            entry.ch.postMessage(message);
            return;
        }
        const ch = new BroadcastChannel(channelName);
        try { ch.postMessage(message); } finally { ch.close(); }
    }

    function subscribe(dotNetRef: any, listenerId: string, channelName: string) {
        if (!('BroadcastChannel' in window)) return;
        const entry = getChannel(channelName);
        const onMessage = (e: MessageEvent) => {
            butil.utils.dispatch(dotNetRef, 'InvokeBroadcastChannelMessage', listenerId, e.data ?? null);
        };
        const onError = () => {
            butil.utils.dispatch(dotNetRef, 'InvokeBroadcastChannelError', listenerId);
        };
        entry.ch.addEventListener('message', onMessage);
        entry.ch.addEventListener('messageerror', onError);
        entry.subscribers++;

        _subscribers[listenerId] = { name: channelName, onMessage, onError };
    }

    function unsubscribe(listenerId: string) {
        const sub = _subscribers[listenerId];
        if (!sub) return;
        delete _subscribers[listenerId];
        const entry = _channels[sub.name];
        if (!entry) return;
        try { entry.ch.removeEventListener('message', sub.onMessage); } catch { /* ignore */ }
        try { entry.ch.removeEventListener('messageerror', sub.onError); } catch { /* ignore */ }
        entry.subscribers--;
        if (entry.subscribers <= 0) {
            try { entry.ch.close(); } catch { /* ignore */ }
            delete _channels[sub.name];
        }
    }
}(BitButil));
