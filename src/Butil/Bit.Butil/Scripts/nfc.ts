var BitButil = BitButil || {};

(function (butil: any) {
    const _readers: { [id: string]: { reader: any, controller: AbortController } } = {};

    butil.nfc = {
        isSupported() { return 'NDEFReader' in window; },
        scan,
        stop,
        writeText,
        writeUrl
    };

    function decodeRecord(rec: any) {
        const out: any = {
            recordType: rec.recordType,
            mediaType: rec.mediaType ?? null,
            id: rec.id ?? null,
            lang: rec.lang ?? null,
            encoding: rec.encoding ?? null,
            text: null,
            data: null
        };
        try {
            if (rec.recordType === 'text' || rec.recordType === 'url' || rec.recordType === 'absolute-url') {
                const decoder = new TextDecoder(rec.encoding || 'utf-8');
                out.text = decoder.decode(rec.data);
            } else if (rec.data) {
                out.data = new Uint8Array(rec.data.buffer || rec.data);
            }
        } catch { /* unsupported encoding - leave fields null */ }
        return out;
    }

    async function scan(id: string, dotNetRef: any) {
        // Defensive: abort/replace any existing reader registered under this id so
        // we don't leak the previous reader's AbortController.
        stop(id);

        const W = window as any;
        if (typeof W.NDEFReader !== 'function') {
            butil.utils.dispatch(dotNetRef, 'InvokeNdefError', id, 'NFC is not supported.');
            return;
        }
        const reader = new W.NDEFReader();
        const controller = new AbortController();
        reader.onreading = (event: any) => {
            butil.utils.dispatch(dotNetRef, 'InvokeNdefReading', id, {
                serialNumber: event.serialNumber ?? '',
                records: (event.message?.records ?? []).map(decodeRecord)
            });
        };
        reader.onreadingerror = () => {
            // A reading error is non-terminal: the scan stays active and may read
            // subsequent tags, so we intentionally keep _readers[id]. Teardown
            // happens via stop() (AbortController) on the .NET side.
            butil.utils.dispatch(dotNetRef, 'InvokeNdefError', id, 'reading-error');
        };
        try { await reader.scan({ signal: controller.signal }); _readers[id] = { reader, controller }; }
        catch (e: any) {
            butil.utils.dispatch(dotNetRef, 'InvokeNdefError', id, e?.message ?? String(e));
        }
    }

    function stop(id: string) {
        const entry = _readers[id];
        if (!entry) return;
        delete _readers[id];
        try { entry.controller.abort(); } catch { /* already aborted */ }
    }

    async function writeText(text: string, lang: string | null, recordId: string | null) {
        const W = window as any;
        if (typeof W.NDEFReader !== 'function') return false;
        try {
            const writer = new W.NDEFReader();
            await writer.write({
                records: [{ recordType: 'text', data: text, lang: lang ?? undefined, id: recordId ?? undefined }]
            });
            return true;
        } catch { return false; }
    }

    async function writeUrl(url: string, recordId: string | null) {
        const W = window as any;
        if (typeof W.NDEFReader !== 'function') return false;
        try {
            const writer = new W.NDEFReader();
            await writer.write({
                records: [{ recordType: 'url', data: url, id: recordId ?? undefined }]
            });
            return true;
        } catch { return false; }
    }
}(BitButil));
