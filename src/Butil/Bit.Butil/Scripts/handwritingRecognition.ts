var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.handwritingRecognition = {
        isSupported() { return typeof (window.navigator as any).createHandwritingRecognizer === 'function'; },

        // The query answers per feature: a runtime that supports handwriting at all still says no to
        // a language it has no model for, and no to hints it doesn't implement.
        async querySupport(languages: string[], alternatives: boolean, textContext: boolean) {
            const query = (window.navigator as any).queryHandwritingRecognizerSupport;
            if (typeof query !== 'function') return null;
            try {
                const result = await query.call(window.navigator, {
                    languages: languages?.length ? languages : ['en'],
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
                        // t is the milliseconds since the stroke started. It is optional in the spec
                        // but materially improves the result, so it is passed whenever it is there.
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
