var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Session {
        transport: any;
        // Held per session so a later openStream call doesn't have to be handed the .NET reference
        // again - the streams it opens dispatch their data through the same one.
        dotNetRef: any;
        streams: { [streamId: string]: { writer: any; readable: any } };
        nextStream: number;
        closed: boolean;
    }

    const _sessions: { [id: string]: Session } = {};

    function toBytes(value: any): Uint8Array {
        if (value instanceof Uint8Array) return value;
        return new Uint8Array(value ?? []);
    }

    // A reader loop per stream, per session. Every WebTransport read is a pull, so the only way to
    // surface incoming data to .NET is to keep pulling and dispatch each chunk as it arrives.
    async function pump(dotNetRef: any, id: string, streamId: string, readable: any) {
        const reader = readable.getReader();
        try {
            for (; ;) {
                const { value, done } = await reader.read();
                if (done) break;
                butil.utils.dispatch(dotNetRef, 'InvokeWebTransportStreamData', id, streamId, toBytes(value));
            }
        } catch {
            // The stream was reset or the session went away; the closed callback reports the why.
        } finally {
            try { reader.releaseLock(); } catch { /* already released */ }
            butil.utils.dispatch(dotNetRef, 'InvokeWebTransportStreamEnd', id, streamId);
        }
    }

    async function pumpDatagrams(dotNetRef: any, id: string, transport: any) {
        const reader = transport.datagrams?.readable?.getReader?.();
        if (!reader) return;
        try {
            for (; ;) {
                const { value, done } = await reader.read();
                if (done) break;
                butil.utils.dispatch(dotNetRef, 'InvokeWebTransportDatagram', id, toBytes(value));
            }
        } catch {
            // Datagrams stop with the session.
        }
    }

    async function pumpIncomingStreams(dotNetRef: any, id: string, session: Session, source: any, bidirectional: boolean) {
        if (!source) return;
        const reader = source.getReader();
        try {
            for (; ;) {
                const { value, done } = await reader.read();
                if (done) break;

                const streamId = `s${session.nextStream++}`;
                // A unidirectional stream from the server IS the readable; a bidirectional one
                // carries both halves, and the writable is what lets .NET answer on it.
                const readable = bidirectional ? value.readable : value;
                session.streams[streamId] = { writer: bidirectional ? value.writable.getWriter() : null, readable };
                butil.utils.dispatch(dotNetRef, 'InvokeWebTransportStreamOpened', id, streamId, bidirectional);
                pump(dotNetRef, id, streamId, readable);
            }
        } catch {
            // The session ended.
        }
    }

    butil.webTransport = {
        isSupported() { return typeof (window as any).WebTransport === 'function'; },
        async connect(dotNetRef: any, id: string, url: string, allowPooling: boolean, congestionControl: string, certificateHashes: any[]) {
            const WT = (window as any).WebTransport;
            if (typeof WT !== 'function') return { connected: false, error: 'WebTransport is not supported' };

            const options: any = {};
            if (allowPooling) options.allowPooling = true;
            if (congestionControl) options.congestionControl = congestionControl;
            if (certificateHashes?.length) {
                // Only a serverCertificateHashes connection can talk to a server whose certificate
                // no public CA signed - the usual case for a local HTTP/3 endpoint.
                options.serverCertificateHashes = certificateHashes.map(hash => ({
                    algorithm: hash.algorithm || 'sha-256',
                    value: toBytes(hash.value)
                }));
            }

            let transport: any;
            try {
                transport = new WT(url, options);
            } catch (e: any) {
                // A url that is not https, or a malformed one - the only synchronous failure.
                return { connected: false, error: String(e?.message ?? e) };
            }

            const session: Session = { transport, dotNetRef, streams: {}, nextStream: 1, closed: false };
            _sessions[id] = session;

            transport.closed
                .then((closeInfo: any) => {
                    session.closed = true;
                    butil.utils.dispatch(dotNetRef, 'InvokeWebTransportClosed', id, closeInfo?.closeCode ?? 0, closeInfo?.reason ?? '', '');
                })
                .catch((e: any) => {
                    session.closed = true;
                    butil.utils.dispatch(dotNetRef, 'InvokeWebTransportClosed', id, 0, '', String(e?.message ?? e));
                });

            try {
                await transport.ready;
            } catch (e: any) {
                delete _sessions[id];
                return { connected: false, error: String(e?.message ?? e) };
            }

            pumpDatagrams(dotNetRef, id, transport);
            pumpIncomingStreams(dotNetRef, id, session, transport.incomingUnidirectionalStreams, false);
            pumpIncomingStreams(dotNetRef, id, session, transport.incomingBidirectionalStreams, true);

            return { connected: true, error: '' };
        },
        async sendDatagram(id: string, data: Uint8Array) {
            const session = _sessions[id];
            if (!session || session.closed) return false;
            try {
                const writer = session.transport.datagrams.writable.getWriter();
                try { await writer.write(data); } finally { writer.releaseLock(); }
                return true;
            } catch {
                // Datagrams are unreliable by design: one too large for the path MTU, or sent into
                // a full queue, is dropped rather than delivered.
                return false;
            }
        },
        async openStream(id: string, bidirectional: boolean) {
            const session = _sessions[id];
            if (!session || session.closed) return null;
            try {
                const streamId = `s${session.nextStream++}`;
                if (bidirectional) {
                    const stream = await session.transport.createBidirectionalStream();
                    session.streams[streamId] = { writer: stream.writable.getWriter(), readable: stream.readable };
                    pump(session.dotNetRef, id, streamId, stream.readable);
                } else {
                    const writable = await session.transport.createUnidirectionalStream();
                    session.streams[streamId] = { writer: writable.getWriter(), readable: null };
                }
                return streamId;
            } catch {
                return null;
            }
        },
        async writeStream(id: string, streamId: string, data: Uint8Array) {
            const stream = _sessions[id]?.streams[streamId];
            if (!stream?.writer) return false;
            try { await stream.writer.write(data); return true; } catch { return false; }
        },
        async closeStream(id: string, streamId: string) {
            const session = _sessions[id];
            const stream = session?.streams[streamId];
            if (!stream) return false;
            delete session.streams[streamId];
            try { await stream.writer?.close(); } catch { /* already closed or reset */ }
            return true;
        },
        state(id: string) {
            const session = _sessions[id];
            if (!session) return 'closed';
            return session.closed ? 'closed' : 'open';
        },
        async close(id: string, closeCode: number, reason: string) {
            const session = _sessions[id];
            if (!session) return;
            delete _sessions[id];
            session.closed = true;
            for (const streamId of Object.keys(session.streams)) {
                try { await session.streams[streamId].writer?.close(); } catch { /* already gone */ }
            }
            try { session.transport.close({ closeCode, reason }); } catch { /* already closed */ }
        },
        async disposeAll() {
            for (const id of Object.keys(_sessions)) await butil.webTransport.close(id, 0, '');
        }
    };
}(BitButil));
