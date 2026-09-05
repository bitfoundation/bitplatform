var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface PeerEntry { pc: any; remoteStream: MediaStream; }
    // The peer a channel belongs to is kept here because closing a connection has to announce its
    // channels itself - see close().
    interface ChannelEntry { channel: any; peerId: string; dotNetRef: any; }

    const _peers: { [id: string]: PeerEntry } = {};
    const _channels: { [id: string]: ChannelEntry } = {};

    function channelOf(channelId: string) { return _channels[channelId]?.channel; }

    function wireChannel(dotNetRef: any, peerId: string, channelId: string, channel: any) {
        _channels[channelId] = { channel, peerId, dotNetRef };
        // Binary arrives as an ArrayBuffer rather than a Blob, for the same reason as on a
        // WebSocket: a Blob would need an extra asynchronous read per message.
        channel.binaryType = 'arraybuffer';

        channel.addEventListener('open', () => butil.utils.dispatch(dotNetRef, 'InvokeChannelOpen', channelId));
        channel.addEventListener('close', () => {
            delete _channels[channelId];
            butil.utils.dispatch(dotNetRef, 'InvokeChannelClose', channelId);
        });
        // Through the shared encoder, so a data-channel message keeps the ButilMessage contract the
        // ports and workers already keep: binary stays binary and everything else is valid JSON,
        // which is what makes Deserialize<T>() work on a payload the peer sent as a plain string.
        channel.addEventListener('message', (e: MessageEvent) =>
            butil.utils.dispatch(dotNetRef, 'InvokeChannelMessage', channelId, ...butil.utils.encodeMessage(e.data)));
    }

    butil.webRtc = {
        isSupported() { return typeof (window as any).RTCPeerConnection === 'function'; },

        create(dotNetRef: any, id: string, iceServers: any[]) {
            const PC = (window as any).RTCPeerConnection;
            if (typeof PC !== 'function') return false;

            let pc: any;
            try { pc = new PC({ iceServers: iceServers ?? [] }); } catch { return false; }

            const remoteStream = new MediaStream();
            _peers[id] = { pc, remoteStream };

            // Each candidate is one way the peer might be reachable, discovered asynchronously
            // after the offer or answer was made. A null candidate means gathering is finished -
            // dispatched too, because that is what tells a signalling channel it can stop waiting.
            pc.addEventListener('icecandidate', (e: any) =>
                butil.utils.dispatch(dotNetRef, 'InvokeIceCandidate', id,
                    e.candidate ? JSON.stringify(e.candidate.toJSON()) : null));

            pc.addEventListener('connectionstatechange', () =>
                butil.utils.dispatch(dotNetRef, 'InvokeConnectionState', id, pc.connectionState));

            // Tracks arrive one at a time; collecting them into one stream is what a <video
            // srcObject> actually wants.
            pc.addEventListener('track', (e: any) => {
                for (const track of e.streams?.[0]?.getTracks() ?? [e.track]) {
                    if (track) remoteStream.addTrack(track);
                }
                butil.utils.dispatch(dotNetRef, 'InvokeTrack', id, e.track?.kind ?? '');
            });

            // A channel the *other* side created. It arrives as an event rather than as a return
            // value, which is why this needs a callback at all.
            pc.addEventListener('datachannel', (e: any) => {
                const channelId = butil.utils.randomUUID();
                wireChannel(dotNetRef, id, channelId, e.channel);
                butil.utils.dispatch(dotNetRef, 'InvokeRemoteChannel', id, channelId, e.channel.label);
            });

            return true;
        },

        createChannel(dotNetRef: any, peerId: string, channelId: string, label: string, ordered: boolean, maxRetransmits: number) {
            const entry = _peers[peerId];
            if (!entry) return false;

            try {
                const options: any = { ordered };
                // -1 means "leave it out": a channel with maxRetransmits set is unreliable by
                // definition, and passing 0 would mean "never retransmit" rather than "reliable".
                if (maxRetransmits >= 0) options.maxRetransmits = maxRetransmits;

                wireChannel(dotNetRef, peerId, channelId, entry.pc.createDataChannel(label, options));
                return true;
            } catch { return false; }
        },

        async createOffer(id: string) {
            const entry = _peers[id];
            if (!entry) return null;
            try {
                const offer = await entry.pc.createOffer();
                return { type: offer.type, sdp: offer.sdp, error: null };
            } catch (e: any) { return { type: null, sdp: null, error: e?.message ?? String(e) }; }
        },

        async createAnswer(id: string) {
            const entry = _peers[id];
            if (!entry) return null;
            try {
                const answer = await entry.pc.createAnswer();
                return { type: answer.type, sdp: answer.sdp, error: null };
            } catch (e: any) { return { type: null, sdp: null, error: e?.message ?? String(e) }; }
        },

        async setLocalDescription(id: string, type: string, sdp: string) {
            const entry = _peers[id];
            if (!entry) return 'unknown peer';
            try { await entry.pc.setLocalDescription({ type, sdp }); return null; }
            catch (e: any) { return e?.message ?? String(e); }
        },

        async setRemoteDescription(id: string, type: string, sdp: string) {
            const entry = _peers[id];
            if (!entry) return 'unknown peer';
            try { await entry.pc.setRemoteDescription({ type, sdp }); return null; }
            catch (e: any) { return e?.message ?? String(e); }
        },

        async addIceCandidate(id: string, candidateJson: string | null) {
            const entry = _peers[id];
            if (!entry) return 'unknown peer';
            try {
                // A null candidate is the end-of-gathering signal, and passing it through is how the
                // other end learns there are no more.
                await entry.pc.addIceCandidate(candidateJson ? JSON.parse(candidateJson) : null);
                return null;
            } catch (e: any) { return e?.message ?? String(e); }
        },

        addTracks(id: string, streamId: string) {
            const entry = _peers[id];
            const stream = butil.mediaDevices.getStream(streamId);
            if (!entry || !stream) return false;

            for (const track of stream.getTracks()) entry.pc.addTrack(track, stream);
            return true;
        },

        attachRemote(id: string, element: any) {
            const entry = _peers[id];
            if (!entry || !element) return false;
            element.srcObject = entry.remoteStream;
            return true;
        },

        connectionState(id: string) { return _peers[id]?.pc.connectionState ?? 'closed'; },
        iceConnectionState(id: string) { return _peers[id]?.pc.iceConnectionState ?? 'closed'; },
        signalingState(id: string) { return _peers[id]?.pc.signalingState ?? 'closed'; },

        // The report is a Map of dictionaries whose members differ per stat type, so it is flattened
        // to strings: the shape is not something a C# record could describe honestly.
        async stats(id: string) {
            const entry = _peers[id];
            if (!entry) return [];

            const report = await entry.pc.getStats();
            const result: any[] = [];

            report.forEach((stat: any) => {
                const values: any = {};
                for (const key of Object.keys(stat)) {
                    if (key === 'id' || key === 'type') continue;
                    const value = stat[key];
                    if (value !== null && typeof value === 'object') continue;   // nested reports, not worth flattening
                    values[key] = String(value);
                }
                result.push({ id: stat.id, type: stat.type, values });
            });

            return result;
        },

        sendText(channelId: string, text: string) {
            const channel = channelOf(channelId);
            if (!channel || channel.readyState !== 'open') return false;
            try { channel.send(text); return true; } catch { return false; }
        },

        sendBytes(channelId: string, bytes: Uint8Array) {
            const channel = channelOf(channelId);
            if (!channel || channel.readyState !== 'open') return false;
            try { channel.send(butil.utils.arrayToBuffer(bytes)); return true; } catch { return false; }
        },

        channelState(channelId: string) { return channelOf(channelId)?.readyState ?? 'closed'; },
        channelBuffered(channelId: string) { return channelOf(channelId)?.bufferedAmount ?? 0; },

        closeChannel(channelId: string) {
            const entry = _channels[channelId];
            if (!entry) return;
            delete _channels[channelId];
            try { entry.channel.close(); } catch { /* already closed */ }
        },

        close(id: string) {
            const entry = _peers[id];
            if (!entry) return;
            delete _peers[id];

            // Closing a connection closes its channels "abruptly", without running the closing
            // procedure - so no channel fires a close event and .NET would never hear that they
            // ended. Announce them here, before the connection goes, or their handlers are stranded.
            for (const channelId of Object.keys(_channels)) {
                const channelEntry = _channels[channelId];
                if (channelEntry.peerId !== id) continue;
                delete _channels[channelId];
                butil.utils.dispatch(channelEntry.dotNetRef, 'InvokeChannelClose', channelId);
            }

            try { entry.pc.close(); } catch { /* already closed */ }
        },

        disposeAll() {
            for (const id of Object.keys(_peers)) butil.webRtc.close(id);
            for (const id of Object.keys(_channels)) butil.webRtc.closeChannel(id);
        }
    };
}(BitButil));
