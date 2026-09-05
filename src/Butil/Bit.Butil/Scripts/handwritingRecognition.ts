var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.handwritingRecognition = {
        isSupported() { return typeof (window.navigator as any).createHandwritingRecognizer === 'function'; },

        // The query answers per feature: a runtime that supports handwriting at all still says no to
        // a language it has no model for, and no to hints it doesn't implement.
        async querySupport(languages: string[], alternatives: boolean, textContext: boolean) {
            const requested = languages?.length ? languages : ['en'];

            // The shipped spelling: it takes the languages alone and answers with a descriptor -
            // null when there is no model for them - whose `hints` say which hints are honoured.
            const query = (window.navigator as any).queryHandwritingRecognizer;
            if (typeof query === 'function') {
                try {
                    const result = await query.call(window.navigator, { languages: requested });
                    if (!result) return { languages: false, alternatives: false, textContext: false };
                    return {
                        languages: true,
                        alternatives: result.hints?.alternatives === true,
                        textContext: result.hints?.textContext === true
                    };
                } catch {
                    return null;
                }
            }

            // The earlier draft's spelling, which asked per feature and answered per feature. Still
            // what a build predating the rename exposes.
            const legacy = (window.navigator as any).queryHandwritingRecognizerSupport;
            if (typeof legacy !== 'function') return null;
            try {
                const result = await legacy.call(window.navigator, {
                    languages: requested,
                    alternatives,
                    textContext
                });
                if (!result) return null;
                return {
                    languages: result.languages === true,
                    alternatives: result.alternatives === true,
                    textContext: result.textContext === true
                };
            } catch {
                return null;
            }
        },

        // One call per recognition rather than a long-lived recognizer handle: the recognizer holds a
        // platform model open, and the strokes of one piece of handwriting are all known by the time
        // .NET asks for a result - there is nothing to keep between calls.
        async recognize(strokes: any[], languages: string[], recognitionType: string, inputType: string, textContext: string, alternatives: number) {
            const create = (window.navigator as any).createHandwritingRecognizer;
            const StrokeCtor = (window as any).HandwritingStroke;
            if (typeof create !== 'function' || typeof StrokeCtor !== 'function') return [];
            if (!strokes?.length) return [];

            let recognizer: any;
            try {
                recognizer = await create.call(window.navigator, { languages: languages?.length ? languages : ['en'] });
            } catch {
                // No model for the requested language, or the feature is off in this build.
                return [];
            }

            let drawing: any;
            try {
                const hints: any = {
                    recognitionType: recognitionType || 'text',
                    inputType: inputType || 'mouse',
                    alternatives: alternatives > 0 ? alternatives : 1
                };
                if (textContext) hints.textContext = textContext;

                drawing = recognizer.startDrawing(hints);

                for (const stroke of strokes) {
                    const handwritingStroke = new StrokeCtor();
                    for (const point of stroke.points ?? []) {
                        // t is the milliseconds since the whole drawing started - one clock across
                        // every stroke, so the pauses between strokes are part of what the recognizer
                        // sees. It is optional in the spec but materially improves the result, so it
                        // is passed through whenever it is there.
                        handwritingStroke.addPoint(point.t === null || point.t === undefined
                            ? { x: point.x, y: point.y }
                            : { x: point.x, y: point.y, t: point.t });
                    }
                    drawing.addStroke(handwritingStroke);
                }

                // Best guess first, then the alternatives. Only the text is carried across: the
                // segmentation result the spec also returns maps characters back to the strokes that
                // produced them, which is of no use once the strokes are back in .NET as plain data.
                const predictions = await drawing.getPrediction();
                return (predictions ?? []).map((prediction: any) => prediction.text ?? '');
            } catch {
                return [];
            } finally {
                // Both hold platform resources; finish() is what releases them, and neither is
                // reachable again once this call returns.
                try { drawing?.finish?.(); } catch { /* already finished */ }
                try { recognizer?.finish?.(); } catch { /* already finished */ }
            }
        }
    };
}(BitButil));
