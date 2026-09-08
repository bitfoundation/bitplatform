var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface KeysEntry {
        mediaKeys: any;
        keySystem: string;
        element: any;
        sessions: { [sessionId: string]: SessionEntry };
    }

    interface SessionEntry {
        session: any;
        message: (e: any) => void;
        statuses: () => void;
    }

    interface EncryptedListener { element: any; handler: (e: any) => void; }

    const _keys: { [id: string]: KeysEntry } = {};
    const _encrypted: { [listenerId: string]: EncryptedListener } = {};

    butil.encryptedMedia = {
        isSupported() { return !!(navigator as any).requestMediaKeySystemAccess; },
        isKeySystemSupported,
        createMediaKeys,
        attach,
        setServerCertificate,
        createSession,
        generateRequest,
        loadSession,
        update,
        sessionId,
        keyStatuses,
        expiration,
        closeSession,
        removeSession,
        subscribeEncrypted,
        unsubscribeEncrypted,
        dispose,
        disposeAll
    };

    // requestMediaKeySystemAccess picks the first configuration it can satisfy and fills in the parts
    // the caller left open, so what comes back is the configuration that would actually be used -
    // which is the answer worth reporting, not merely "yes".
    async function requestAccess(keySystem: string, configurations: any[]) {
        const request = (navigator as any).requestMediaKeySystemAccess;
        if (!request) return null;
        try {
            return await request.call(navigator, keySystem, (configurations ?? []).map(toJsConfiguration));
        } catch {
            // NotSupportedError for an unknown key system, or none of the configurations matched.
            return null;
        }
    }

    function toJsConfiguration(configuration: any) {
        const result: any = butil.utils.pick(configuration, ['label', 'initDataTypes', 'distinctiveIdentifier', 'persistentState', 'sessionTypes']);
        if (configuration?.audioCapabilities?.length) result.audioCapabilities = configuration.audioCapabilities.map(toJsCapability);
        if (configuration?.videoCapabilities?.length) result.videoCapabilities = configuration.videoCapabilities.map(toJsCapability);
        return result;
    }

    function toJsCapability(capability: any) {
        return butil.utils.pick(capability, ['contentType', 'robustness', 'encryptionScheme']);
    }

    async function isKeySystemSupported(keySystem: string, configurations: any[]) {
        const access = await requestAccess(keySystem, configurations);
        if (!access) return null;
        return { keySystem: access.keySystem, configuration: access.getConfiguration() };
    }

    async function createMediaKeys(id: string, keySystem: string, configurations: any[]) {
        const access = await requestAccess(keySystem, configurations);
        if (!access) return null;

        try {
            const mediaKeys = await access.createMediaKeys();
            dispose(id);
            _keys[id] = { mediaKeys, keySystem: access.keySystem, element: null, sessions: {} };
            return { keySystem: access.keySystem, configuration: access.getConfiguration() };
        } catch {
            // The CDM refused to instantiate - unavailable, or blocked by the embedder's policy.
            return null;
        }
    }

    async function attach(id: string, element: any) {
        const entry = _keys[id];
        if (!entry || !element?.setMediaKeys) return false;
        try {
            await element.setMediaKeys(entry.mediaKeys);
            entry.element = element;
            return true;
        } catch {
            // An element that already has different keys attached, or a CDM the element rejects.
            return false;
        }
    }

    async function setServerCertificate(id: string, certificate: Uint8Array) {
        const entry = _keys[id];
        if (!entry?.mediaKeys.setServerCertificate) return false;
        try { return !!(await entry.mediaKeys.setServerCertificate(butil.utils.arrayToBuffer(certificate))); }
        catch { return false; }
    }

    function createSession(id: string, sid: string, sessionType: string, dotNetRef: any, messageMethod: string, statusMethod: string) {
        const entry = _keys[id];
        if (!entry) return false;
        try {
            const session = entry.mediaKeys.createSession(sessionType || 'temporary');

            // The license request only ever arrives as a 'message' event; there is no way to ask a
            // session for it. A session with nobody listening is a session that can never be licensed.
            const message = (e: any) => butil.utils.dispatch(dotNetRef, messageMethod, sid, e.messageType, new Uint8Array(e.message));
            // Two parallel arrays rather than an array of objects: a [JSInvokable] callback can only
            // take publicly visible parameter types, and the key-id/status pair has no public shape.
            const statuses = () => {
                const list = readStatuses(session);
                butil.utils.dispatch(dotNetRef, statusMethod, sid, list.map(e => e.keyId), list.map(e => e.status));
            };
            session.addEventListener('message', message);
            session.addEventListener('keystatuseschange', statuses);

            entry.sessions[sid] = { session, message, statuses };
            return true;
        } catch {
            // The key system doesn't support the requested session type.
            return false;
        }
    }

    async function generateRequest(id: string, sid: string, initDataType: string, initData: Uint8Array) {
        const session = _keys[id]?.sessions[sid]?.session;
        if (!session) return false;
        try { await session.generateRequest(initDataType, butil.utils.arrayToBuffer(initData)); return true; }
        catch { return false; }
    }

    async function loadSession(id: string, sid: string, storedSessionId: string) {
        const session = _keys[id]?.sessions[sid]?.session;
        if (!session) return false;
        try { return !!(await session.load(storedSessionId)); }
        catch { return false; }
    }

    async function update(id: string, sid: string, response: Uint8Array) {
        const session = _keys[id]?.sessions[sid]?.session;
        if (!session) return false;
        try { await session.update(butil.utils.arrayToBuffer(response)); return true; }
        catch { return false; }
    }

    function sessionId(id: string, sid: string) {
        return _keys[id]?.sessions[sid]?.session.sessionId ?? '';
    }

    function keyStatuses(id: string, sid: string) {
        const session = _keys[id]?.sessions[sid]?.session;
        return session ? readStatuses(session) : [];
    }

    function readStatuses(session: any) {
        const result: { keyId: string, status: string }[] = [];
        try {
            session.keyStatuses.forEach((status: string, keyId: any) => {
                // Key ids are opaque binary; hex is what license servers and logs use for them, and
                // it is comparable in .NET without a second round trip.
                result.push({ keyId: toHex(keyId), status });
            });
        } catch { /* the session was closed underneath us */ }
        return result;
    }

    function expiration(id: string, sid: string) {
        const value = _keys[id]?.sessions[sid]?.session.expiration;
        // A licence with no expiry reports NaN, which does not survive JSON.
        return (typeof value === 'number' && isFinite(value)) ? value : null;
    }

    async function closeSession(id: string, sid: string) {
        const entry = _keys[id];
        const session = entry?.sessions[sid];
        if (!entry || !session) return;
        delete entry.sessions[sid];
        detachSession(session);
        try { await session.session.close(); } catch { /* already closed */ }
    }

    async function removeSession(id: string, sid: string) {
        const session = _keys[id]?.sessions[sid]?.session;
        if (!session) return false;
        try { await session.remove(); return true; } catch { return false; }
    }

    function detachSession(session: SessionEntry) {
        try {
            session.session.removeEventListener('message', session.message);
            session.session.removeEventListener('keystatuseschange', session.statuses);
        } catch { /* the session object is gone */ }
    }

    // The 'encrypted' event is how a page learns that the media it is playing needs a key, and it
    // carries the initialization data the license request has to be generated from. Watching it is
    // the only alternative to parsing the container in .NET.
    function subscribeEncrypted(listenerId: string, element: any, dotNetRef: any, method: string) {
        if (!element) return;
        const handler = (e: any) => butil.utils.dispatch(dotNetRef, method, listenerId, e.initDataType ?? '', e.initData ? new Uint8Array(e.initData) : new Uint8Array(0));
        _encrypted[listenerId] = { element, handler };
        element.addEventListener('encrypted', handler);
    }

    function unsubscribeEncrypted(listenerId: string) {
        const entry = _encrypted[listenerId];
        if (!entry) return;
        delete _encrypted[listenerId];
        try { entry.element.removeEventListener('encrypted', entry.handler); } catch { /* element already gone */ }
    }

    function dispose(id: string) {
        const entry = _keys[id];
        if (!entry) return;
        delete _keys[id];

        for (const sid of Object.keys(entry.sessions)) {
            const session = entry.sessions[sid];
            detachSession(session);
            try { session.session.close(); } catch { /* already closed */ }
        }

        // Detaching the keys is what lets the CDM release the hardware resources it reserved; an
        // element left holding them can keep a secure decode pipeline open for the whole page.
        try { entry.element?.setMediaKeys?.(null); } catch { /* element already gone */ }
    }

    function disposeAll() {
        for (const id of Object.keys(_keys)) dispose(id);
        for (const listenerId of Object.keys(_encrypted)) unsubscribeEncrypted(listenerId);
    }

    function toHex(keyId: any) {
        const bytes = keyId instanceof Uint8Array ? keyId : new Uint8Array(keyId);
        return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
    }
}(BitButil));
