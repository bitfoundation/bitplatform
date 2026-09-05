var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _trails: { [id: string]: { element: Element, handler: (event: PointerEvent) => void, style: any } } = {};
    // Requesting a presenter is asynchronous, so a stop() can land while one is still being
    // requested. The counter says which start is still the current one for an id.
    const _starts: { [id: string]: number } = {};

    butil.ink = {
        isSupported() { return typeof (window.navigator as any).ink?.requestPresenter === 'function'; },

        // The presenter has to be fed the browser's own PointerEvent object, which is exactly what
        // cannot cross the interop boundary - by the time .NET saw one it would be a copy, and an
        // untrusted one at that. So the listener lives here and .NET only turns it on and off.
        async start(id: string, element: Element, color: string, diameter: number) {
            const ink = (window.navigator as any).ink;
            if (typeof ink?.requestPresenter !== 'function' || !element) return false;

            butil.ink.stop(id);

            const token = _starts[id] = (_starts[id] ?? 0) + 1;

            let presenter: any;
            try {
                presenter = await ink.requestPresenter({ presentationArea: element });
            } catch {
                // Delegated ink is off, or the element is not a valid presentation area.
                return false;
            }

            // A stop() - or another start() - ran while the presenter was on its way: this one is
            // stale, and registering its listener would draw a trail nobody asked for any more.
            if (_starts[id] !== token) return false;

            const style = { color: color || 'black', diameter: diameter > 0 ? diameter : 3 };
            const handler = (event: PointerEvent) => {
                try { presenter.updateInkTrailStartPoint(event, style); } catch { /* the presenter went away */ }
            };

            _trails[id] = { element, handler, style };
            element.addEventListener('pointermove', handler as EventListener);
            return true;
        },

        // The style object is the same one the handler passes on every move, so mutating it changes
        // the trail from the next point onwards without re-requesting a presenter.
        setStyle(id: string, color: string, diameter: number) {
            const trail = _trails[id];
            if (!trail) return false;
            trail.style.color = color || trail.style.color;
            trail.style.diameter = diameter > 0 ? diameter : trail.style.diameter;
            return true;
        },

        stop(id: string) {
            // Bumped even with no trail to remove: a start still awaiting its presenter has to see
            // that it was stopped.
            if (_starts[id]) _starts[id]++;

            const trail = _trails[id];
            if (!trail) return;
            delete _trails[id];
            trail.element.removeEventListener('pointermove', trail.handler as EventListener);
        },
        disposeAll() {
            for (const id of Object.keys(_starts)) butil.ink.stop(id);
            for (const id of Object.keys(_trails)) butil.ink.stop(id);
        }
    };
}(BitButil));
