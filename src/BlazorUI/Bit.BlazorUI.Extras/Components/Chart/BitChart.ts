namespace BitBlazorUI {
    interface BitChartZoomOptions {
        wheel: boolean;
        pan: boolean;
        drag: boolean;
    }

    // BitChart pointer interop — used for responsive sizing (always) and zoom/pan (optional).
    // Computes normalized cursor fractions and forwards wheel/drag gestures to .NET. All drawing
    // (geometry, scales, layout, animation) is done in C# and rendered as plain SVG.
    export class BitChart {
        // Observe an element's pixel size and report changes to .NET so the chart can render at real
        // device pixels (keeping font sizes constant, like Chart.js) instead of scaling a fixed viewBox.
        public static observe(element: HTMLElement, dotnet: DotNetObject) {
            let lastW = 0, lastH = 0;

            function report() {
                const r = element.getBoundingClientRect();
                const w = Math.round(r.width), h = Math.round(r.height);
                if (w > 0 && h > 0 && (Math.abs(w - lastW) > 1 || Math.abs(h - lastH) > 1)) {
                    lastW = w; lastH = h;
                    dotnet.invokeMethodAsync('OnResize', w, h);
                }
            }

            let ro: ResizeObserver | null = null;
            if (typeof ResizeObserver !== 'undefined') {
                ro = new ResizeObserver(report);
                ro.observe(element);
            } else {
                window.addEventListener('resize', report);
            }
            report();

            return {
                dispose() {
                    if (ro) ro.disconnect();
                    else window.removeEventListener('resize', report);
                }
            };
        }

        public static register(element: HTMLElement, dotnet: DotNetObject, opts: BitChartZoomOptions) {
            const state = { panning: false, lastX: 0, lastY: 0, startX: 0, startY: 0 };

            function frac(e: { clientX: number, clientY: number }) {
                const r = element.getBoundingClientRect();
                return {
                    x: r.width ? (e.clientX - r.left) / r.width : 0.5,
                    y: r.height ? (e.clientY - r.top) / r.height : 0.5
                };
            }

            function onWheel(e: WheelEvent) {
                if (!opts.wheel) return;
                e.preventDefault();
                const f = frac(e);
                dotnet.invokeMethodAsync('OnWheelZoom', f.x, f.y, e.deltaY);
            }

            function onDown(e: PointerEvent) {
                if (e.button !== 0) return;
                if (!opts.pan && !opts.drag) return;
                state.panning = true;
                state.lastX = e.clientX;
                state.lastY = e.clientY;
                state.startX = e.clientX;
                state.startY = e.clientY;
                if (!opts.drag) element.style.cursor = 'grabbing';
                try { element.setPointerCapture(e.pointerId); } catch { }
            }

            function onMove(e: PointerEvent) {
                if (!state.panning) return;
                const r = element.getBoundingClientRect();
                if (opts.drag) {
                    const x0 = r.width ? (state.startX - r.left) / r.width : 0;
                    const y0 = r.height ? (state.startY - r.top) / r.height : 0;
                    const x1 = r.width ? (e.clientX - r.left) / r.width : 0;
                    const y1 = r.height ? (e.clientY - r.top) / r.height : 0;
                    dotnet.invokeMethodAsync('OnDragMove', x0, y0, x1, y1);
                    return;
                }
                const dx = r.width ? (e.clientX - state.lastX) / r.width : 0;
                const dy = r.height ? (e.clientY - state.lastY) / r.height : 0;
                state.lastX = e.clientX;
                state.lastY = e.clientY;
                dotnet.invokeMethodAsync('OnPan', dx, dy);
            }

            function onUp(e: PointerEvent) {
                if (!state.panning) return;
                state.panning = false;
                element.style.cursor = '';
                if (opts.drag) {
                    const r = element.getBoundingClientRect();
                    const x0 = r.width ? (state.startX - r.left) / r.width : 0;
                    const y0 = r.height ? (state.startY - r.top) / r.height : 0;
                    const x1 = r.width ? (e.clientX - r.left) / r.width : 0;
                    const y1 = r.height ? (e.clientY - r.top) / r.height : 0;
                    dotnet.invokeMethodAsync('OnDragEnd', x0, y0, x1, y1);
                }
            }

            function onDouble() {
                dotnet.invokeMethodAsync('OnResetZoom');
            }

            element.addEventListener('wheel', onWheel, { passive: false });
            element.addEventListener('pointerdown', onDown);
            element.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp);
            element.addEventListener('dblclick', onDouble);

            return {
                dispose() {
                    element.removeEventListener('wheel', onWheel);
                    element.removeEventListener('pointerdown', onDown);
                    element.removeEventListener('pointermove', onMove);
                    window.removeEventListener('pointerup', onUp);
                    element.removeEventListener('dblclick', onDouble);
                }
            };
        }
    }
}
