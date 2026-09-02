var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.privacySandbox = {
        // --- Topics ---------------------------------------------------------------------------
        isTopicsSupported() { return typeof (document as any).browsingTopics === 'function'; },
        // Returns at most three topics, and only ones this caller has already observed the user on -
        // an empty array is the normal answer, not a failure.
        async getTopics(skipObservation: boolean) {
            const browsingTopics = (document as any).browsingTopics;
            if (typeof browsingTopics !== 'function') return [];
            try {
                const topics = await browsingTopics.call(document, { skipObservation });
                return (topics ?? []).map((topic: any) => ({
                    topic: topic.topic ?? 0,
                    version: topic.version ?? '',
                    configVersion: topic.configVersion ?? '',
                    modelVersion: topic.modelVersion ?? '',
                    taxonomyVersion: topic.taxonomyVersion ?? ''
                }));
            } catch {
                // The permissions policy blocks it, or the user turned the feature off.
                return [];
            }
        },

        // --- Attribution Reporting -------------------------------------------------------------
        isAttributionReportingSupported() {
            return typeof (window as any).AttributionReporting !== 'undefined'
                || (document as any).featurePolicy?.features?.().includes?.('attribution-reporting') === true
                || 'attributionSrc' in document.createElement('img');
        },
        // The registration is the request's response headers, not its body, so nothing is returned:
        // the browser reads Attribution-Reporting-Register-Source off the response and stores it.
        async registerSource(url: string, eventSourceEligible: boolean, triggerEligible: boolean) {
            try {
                await fetch(url, {
                    keepalive: true,
                    attributionReporting: { eventSourceEligible, triggerEligible }
                } as any);
                return true;
            } catch {
                return false;
            }
        },
        // The image form, which is what an ad creative uses: the browser registers off the image
        // response and the image itself is never displayed.
        registerSourceImage(url: string) {
            const image = document.createElement('img');
            if (!('attributionSrc' in image)) return false;
            try {
                (image as any).attributionSrc = url;
                image.src = url;
                return true;
            } catch {
                return false;
            }
        },

        // --- Private State Tokens ---------------------------------------------------------------
        isPrivateStateTokensSupported() { return typeof (document as any).hasPrivateToken === 'function'; },
        async hasPrivateToken(issuer: string) {
            const has = (document as any).hasPrivateToken;
            if (typeof has !== 'function') return false;
            try { return await has.call(document, issuer); } catch { return false; }
        },
        async hasRedemptionRecord(issuer: string) {
            const has = (document as any).hasRedemptionRecord;
            if (typeof has !== 'function') return false;
            try { return await has.call(document, issuer); } catch { return false; }
        },
        // One fetch carrying a token operation. 'token-request' asks the issuer for tokens,
        // 'token-redemption' spends one, and 'send-redemption-record' attaches the proof.
        async requestToken(url: string, operation: string, version: number) {
            try {
                await fetch(url, {
                    privateToken: { version: version > 0 ? version : 1, operation }
                } as any);
                return true;
            } catch {
                return false;
            }
        },

        // --- Fenced Frames -----------------------------------------------------------------------
        isFencedFrameSupported() { return typeof (window as any).HTMLFencedFrameElement === 'function'; },
        // Whether this document is itself running inside a fenced frame - which is the thing an app
        // has to know, because storage, navigation and referrers all behave differently in one.
        isInFencedFrame() { return typeof (window as any).fence !== 'undefined'; }
    };
}(BitButil));
