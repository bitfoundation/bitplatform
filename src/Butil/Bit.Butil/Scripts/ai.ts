var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One module serves all seven built-in AI APIs. They are the same object three times over -
    // a static `availability()`, a static `create()` with a download monitor, and a session with
    // one run method - so the alternative would be seven near-identical modules whose only
    // difference is a global name and the name of the method to call on the session.
    const _sessions: { [id: string]: { api: string, session: any } } = {};

    // The run method of each API's session object, and whether its streaming twin exists.
    const RUN: { [api: string]: string } = {
        languageModel: 'prompt',
        summarizer: 'summarize',
        translator: 'translate',
        languageDetector: 'detect',
        writer: 'write',
        rewriter: 'rewrite',
        proofreader: 'proofread'
    };

    // Resolve the API's factory. The shipped shape is a global constructor (`LanguageModel`);
    // earlier Chromium builds hung the same objects off `self.ai.*`, which some channels still do.
    const LEGACY: { [api: string]: string } = {
        languageModel: 'languageModel',
        summarizer: 'summarizer',
        translator: 'translator',
        languageDetector: 'languageDetector',
        writer: 'writer',
        rewriter: 'rewriter',
        proofreader: 'proofreader'
    };

    function factory(api: string) {
        const global = api.charAt(0).toUpperCase() + api.slice(1);
        return (window as any)[global] ?? (window as any).ai?.[LEGACY[api]] ?? null;
    }

    // Members left null by .NET are dropped so a runtime never sees an option it doesn't know, and
    // so an unset option doesn't override the model's own default.
    function clean(options: any) {
        const result: any = {};
        if (!options) return result;
        for (const key of Object.keys(options)) {
            const value = options[key];
            if (value === null || value === undefined) continue;
            if (Array.isArray(value) && value.length === 0) continue;
            result[key] = value;
        }
        return result;
    }

    function toMessages(prompts: any[]) {
        return (prompts ?? []).map(p => ({ role: p.role, content: p.content }));
    }

    butil.ai = {
        isSupported(api: string) { return !!factory(api); },
        async availability(api: string, options: any) {
            const f = factory(api);
            if (!f?.availability) return 'unavailable';
            try {
                return await f.availability(clean(options)) ?? 'unavailable';
            } catch {
                // An option set the runtime can't serve (an unsupported language pair, say) is
                // reported as unavailable rather than thrown - it is the same answer.
                return 'unavailable';
            }
        },
        async getParams(api: string) {
            const f = factory(api);
            if (!f?.params) return null;
            try {
                const p = await f.params();
                if (!p) return null;
                return {
                    defaultTemperature: p.defaultTemperature ?? 0,
                    maxTemperature: p.maxTemperature ?? 0,
                    defaultTopK: p.defaultTopK ?? 0,
                    maxTopK: p.maxTopK ?? 0
                };
            } catch {
                return null;
            }
        },
        async create(api: string, sessionId: string, options: any, dotNetRef: any, progressId: string, progressMethod: string) {
            const f = factory(api);
            if (!f?.create) return 'unavailable';

            const config: any = clean(options);

            // initialPrompts arrive as {role, content} pairs; a system prompt is the first of them,
            // which is how the spec expresses what earlier drafts called `systemPrompt`.
            if (config.initialPrompts) config.initialPrompts = toMessages(config.initialPrompts);
            if (config.systemPrompt) {
                config.initialPrompts = [{ role: 'system', content: config.systemPrompt }, ...(config.initialPrompts ?? [])];
                delete config.systemPrompt;
            }

            // The language model states expected languages per input/output type rather than as a
            // flat list; every other API takes the flat list directly.
            if (api === 'languageModel') {
                if (config.expectedInputLanguages) {
                    config.expectedInputs = [{ type: 'text', languages: config.expectedInputLanguages }];
                    delete config.expectedInputLanguages;
                }
                if (config.outputLanguage) {
                    config.expectedOutputs = [{ type: 'text', languages: [config.outputLanguage] }];
                    delete config.outputLanguage;
                }
            }

            if (dotNetRef && progressMethod) {
                // The download monitor is the only way to report first-run model download progress,
                // which can be gigabytes and minutes long.
                config.monitor = (m: any) => m.addEventListener('downloadprogress', (e: any) =>
                    butil.utils.dispatch(dotNetRef, progressMethod, progressId, e?.loaded ?? 0));
            }

            try {
                const session = await f.create(config);
                // Never orphan a previous session registered under this id.
                destroy(sessionId);
                _sessions[sessionId] = { api, session };
                return 'created';
            } catch (e: any) {
                // NotSupportedError for an unusable option set, NotAllowedError when the user
                // declined the download, AbortError when it was cancelled.
                return e?.name === 'NotAllowedError' ? 'denied' : 'unavailable';
            }
        },
        async run(sessionId: string, input: string, options: any) {
            const entry = _sessions[sessionId];
            if (!entry) return null;

            const method = RUN[entry.api];
            const fn = entry.session?.[method];
            if (typeof fn !== 'function') return null;

            return await fn.call(entry.session, input, clean(options));
        },
        async runStreaming(sessionId: string, input: string, options: any, dotNetRef: any, streamId: string, chunkMethod: string, doneMethod: string) {
            const entry = _sessions[sessionId];
            if (!entry) {
                butil.utils.dispatch(dotNetRef, doneMethod, streamId, '', 'The AI session is gone.');
                return;
            }

            const method = `${RUN[entry.api]}Streaming`;
            const fn = entry.session?.[method];
            if (typeof fn !== 'function') {
                butil.utils.dispatch(dotNetRef, doneMethod, streamId, '', `This session does not implement ${method}.`);
                return;
            }

            let full = '';
            try {
                const stream = fn.call(entry.session, input, clean(options));
                // The shipped streams yield each chunk as the delta, not the whole text so far.
                for await (const chunk of stream) {
                    const text = typeof chunk === 'string' ? chunk : (chunk?.value ?? '');
                    full += text;
                    butil.utils.dispatch(dotNetRef, chunkMethod, streamId, text);
                }
                butil.utils.dispatch(dotNetRef, doneMethod, streamId, full, '');
            } catch (e: any) {
                butil.utils.dispatch(dotNetRef, doneMethod, streamId, full, e?.message ?? 'The AI stream failed.');
            }
        },
        async detect(sessionId: string, input: string) {
            const entry = _sessions[sessionId];
            if (!entry?.session?.detect) return [];
            const results = await entry.session.detect(input);
            return (results ?? []).map((r: any) => ({
                detectedLanguage: r.detectedLanguage ?? '',
                confidence: r.confidence ?? 0
            }));
        },
        async proofread(sessionId: string, input: string) {
            const entry = _sessions[sessionId];
            if (!entry?.session?.proofread) return null;
            const result = await entry.session.proofread(input);
            return {
                correctedInput: result?.correctedInput ?? '',
                corrections: (result?.corrections ?? []).map((c: any) => ({
                    startIndex: c.startIndex ?? 0,
                    endIndex: c.endIndex ?? 0,
                    correction: c.correction ?? '',
                    type: c.type ?? c.correctionType ?? '',
                    explanation: c.explanation ?? ''
                }))
            };
        },
        async append(sessionId: string, prompts: any[]) {
            const entry = _sessions[sessionId];
            if (typeof entry?.session?.append !== 'function') return false;
            try {
                await entry.session.append(toMessages(prompts));
                return true;
            } catch {
                return false;
            }
        },
        async measureInputUsage(sessionId: string, input: string, options: any) {
            const entry = _sessions[sessionId];
            const fn = entry?.session?.measureInputUsage;
            if (typeof fn !== 'function') return -1;
            try { return await fn.call(entry.session, input, clean(options)) ?? -1; }
            catch { return -1; }
        },
        getUsage(sessionId: string) {
            const session = _sessions[sessionId]?.session;
            if (!session) return null;
            return {
                inputUsage: session.inputUsage ?? 0,
                inputQuota: session.inputQuota ?? 0
            };
        },
        async clone(sessionId: string, newSessionId: string) {
            const entry = _sessions[sessionId];
            if (typeof entry?.session?.clone !== 'function') return false;
            try {
                const clone = await entry.session.clone();
                destroy(newSessionId);
                _sessions[newSessionId] = { api: entry.api, session: clone };
                return true;
            } catch {
                return false;
            }
        },
        destroy,
        disposeAll() {
            for (const id of Object.keys(_sessions)) destroy(id);
        }
    };

    function destroy(sessionId: string) {
        const entry = _sessions[sessionId];
        if (!entry) return;
        delete _sessions[sessionId];
        // A session holds model state; not destroying it keeps that memory alive until GC, which
        // for an on-device model is measured in hundreds of megabytes.
        try { entry.session?.destroy?.(); } catch { /* already destroyed */ }
    }
}(BitButil));
