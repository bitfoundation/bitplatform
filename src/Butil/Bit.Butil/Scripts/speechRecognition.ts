var BitButil = BitButil || {};

(function (butil: any) {
    const _sessions: { [id: string]: any } = {};

    butil.speechRecognition = {
        isSupported() {
            const W = window as any;
            return !!(W.SpeechRecognition || W.webkitSpeechRecognition);
        },
        start,
        stop
    };

    function start(id: string, options: any, resultMethod: string, errorMethod: string, endMethod: string) {
        const W = window as any;
        const Ctor = W.SpeechRecognition || W.webkitSpeechRecognition;
        if (!Ctor) {
            DotNet.invokeMethodAsync('Bit.Butil', errorMethod, id, 'SpeechRecognition is not supported.');
            return;
        }
        const r = new Ctor();
        if (options.lang) r.lang = options.lang;
        r.continuous = !!options.continuous;
        r.interimResults = !!options.interimResults;
        r.maxAlternatives = options.maxAlternatives ?? 1;

        r.onresult = (event: any) => {
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const res = event.results[i];
                // We only forward the top alternative; callers wanting more should bump
                // maxAlternatives and read each result via getEntries-style observation.
                const top = res?.[0];
                if (!top) continue;
                DotNet.invokeMethodAsync('Bit.Butil', resultMethod, id, {
                    transcript: top.transcript ?? '',
                    confidence: top.confidence ?? 0,
                    isFinal: !!res.isFinal
                });
            }
        };
        r.onerror = (event: any) => {
            DotNet.invokeMethodAsync('Bit.Butil', errorMethod, id, event?.error ?? 'unknown');
        };
        r.onend = () => {
            DotNet.invokeMethodAsync('Bit.Butil', endMethod, id);
            delete _sessions[id];
        };

        try { r.start(); _sessions[id] = r; }
        catch (e: any) {
            DotNet.invokeMethodAsync('Bit.Butil', errorMethod, id, e?.message ?? String(e));
        }
    }

    function stop(id: string) {
        const r = _sessions[id];
        if (!r) return;
        delete _sessions[id];
        try { r.stop(); } catch { /* already stopped */ }
    }
}(BitButil));
