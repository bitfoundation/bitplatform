var BitButil = BitButil || {};

(function (butil: any) {
    interface Listener { element: HTMLElement; enter: (e: any) => void; leave: (e: any) => void; }

    const _listeners: { [id: string]: Listener } = {};

    butil.pictureInPicture = {
        isSupported() { return !!(document as any).pictureInPictureEnabled; },
        // A video can opt out per-element, and a stream with no video track can never be
        // picture-in-pictured, so support is a per-element question as well as a per-document one.
        isAvailableFor(element: any) {
            if (!element || element.tagName !== 'VIDEO') return false;
            if (!(document as any).pictureInPictureEnabled) return false;
            if (element.disablePictureInPicture) return false;
            // readyState < HAVE_METADATA means no video track is known yet, and request() would
            // reject; callers usually just need to wait for loadedmetadata.
            return element.readyState >= 1;
        },
        async request(element: any) {
            if (!element?.requestPictureInPicture) return false;
            try {
                await element.requestPictureInPicture();
                return true;
            } catch {
                // Dismissed, blocked by permissions policy, or no video track yet.
                return false;
            }
        },
        async exit() {
            const d = document as any;
            if (!d.pictureInPictureElement || !d.exitPictureInPicture) return false;
            try { await d.exitPictureInPicture(); return true; } catch { return false; }
        },
        isActive(element: any) {
            const current = (document as any).pictureInPictureElement;
            if (!current) return false;
            // With no element the question is "is anything in PiP", with one it is "is this it".
            return element ? current === element : true;
        },
        subscribe(dotNetRef: any, listenerId: string, element: any) {
            if (!element) return;
            // The floating window's size is only ever reported on the enter event - there is no
            // property to read it back from later - so it is forwarded as part of the change.
            const enter = (e: any) => butil.utils.dispatch(dotNetRef, 'InvokePictureInPictureChange', listenerId, true,
                e?.pictureInPictureWindow?.width ?? 0, e?.pictureInPictureWindow?.height ?? 0);
            const leave = () => butil.utils.dispatch(dotNetRef, 'InvokePictureInPictureChange', listenerId, false, 0, 0);
            _listeners[listenerId] = { element, enter, leave };
            element.addEventListener('enterpictureinpicture', enter);
            element.addEventListener('leavepictureinpicture', leave);
        },
        unsubscribe(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            entry.element.removeEventListener('enterpictureinpicture', entry.enter);
            entry.element.removeEventListener('leavepictureinpicture', entry.leave);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.pictureInPicture.unsubscribe(id);
        }
    };
}(BitButil));
