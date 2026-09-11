var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Streaming decoders kept alive between calls. A decoder fed one network chunk at a time has to
    // hold on to the bytes of a character that straddles a chunk boundary, so the instance itself is
    // the state - it cannot be recreated per call.
    const _decoders: { [id: string]: TextDecoder } = {};

    butil.textEncoding = {
        isSupported() { return typeof TextDecoder === 'function'; },
        // A label the runtime doesn't know makes the constructor throw, which is the only way to ask
        // the question - the platform exposes no list of the encodings it implements.
        isEncodingSupported(label: string) {
            try { new TextDecoder(label); return true; } catch { return false; }
        },
        // The canonical name of a label ('shift-jis' -> 'shift_jis'), or null when it is unknown.
        canonicalName(label: string) {
            try { return new TextDecoder(label).encoding; } catch { return null; }
        },
        decode(data: Uint8Array, label: string, fatal: boolean, ignoreBom: boolean) {
            try {
                const decoder = new TextDecoder(label || 'utf-8', { fatal, ignoreBOM: ignoreBom });
                return decoder.decode(butil.utils.arrayToBuffer(data));
            } catch {
                // An unknown label, or - with fatal on - bytes that are not valid in this encoding.
                return null;
            }
        },
        encode(text: string) { return new TextEncoder().encode(text ?? ''); },
        // How many bytes `text` occupies as UTF-8, without moving the bytes across the boundary.
        byteLength(text: string) { return new TextEncoder().encode(text ?? '').length; },

        createDecoder(id: string, label: string, fatal: boolean, ignoreBom: boolean) {
            try {
                _decoders[id] = new TextDecoder(label || 'utf-8', { fatal, ignoreBOM: ignoreBom });
                return true;
            } catch {
                return false;
            }
        },
        // `stream` keeps a partial character pending for the next call; the last call has to pass
        // false so a trailing incomplete sequence is flushed (as U+FFFD, or a throw when fatal).
        decodeChunk(id: string, data: Uint8Array, stream: boolean) {
            const decoder = _decoders[id];
            if (!decoder) return null;
            try { return decoder.decode(butil.utils.arrayToBuffer(data), { stream }); }
            catch { return null; }
        },
        disposeDecoder(id: string) { delete _decoders[id]; },
        disposeAll() {
            for (const id in _decoders) delete _decoders[id];
        }
    };
}(BitButil));
