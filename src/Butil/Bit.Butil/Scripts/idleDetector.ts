var BitButil = BitButil || {};

(function (butil: any) {
    const _detectors: { [id: string]: { detector: any, controller: AbortController } } = {};

    butil.idleDetector = {
        isSupported() { return 'IdleDetector' in window; },
        async requestPermission() {
            const ID: any = (window as any).IdleDetector;
            if (!ID?.requestPermission) return 'unknown';
            try { return await ID.requestPermission(); }
            catch { return 'denied'; }
        },
        async start(dotNetRef: any, listenerId: string, threshold: number) {
            const ID: any = (window as any).IdleDetector;
            if (!ID) return;
            const controller = new AbortController();
            const detector = new ID();

            const fire = () => {
                dotNetRef.invokeMethodAsync('InvokeIdleDetector', listenerId, {
                    userState: detector.userState ?? 'active',
                    screenState: detector.screenState ?? 'unlocked'
                });
            };
            detector.addEventListener('change', fire);

            try {
                await detector.start({ threshold: threshold * 1000, signal: controller.signal });
                _detectors[listenerId] = { detector, controller };
                // Emit an initial snapshot so the dotnet side knows the current state.
                fire();
            } catch {
                // Permission denied or aborted before start.
            }
        },
        stop(listenerId: string) {
            const entry = _detectors[listenerId];
            if (!entry) return;
            delete _detectors[listenerId];
            try { entry.controller.abort(); } catch { /* already aborted */ }
        }
    };
}(BitButil));
