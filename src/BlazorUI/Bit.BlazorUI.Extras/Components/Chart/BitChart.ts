namespace BitBlazorUI {
    interface BitChartZoomOptions {
        wheel: boolean;
        pan: boolean;
        drag: boolean;
    }

    // BitChart pointer interop - used for responsive sizing (always) and zoom/pan (optional).
    // Computes normalized cursor fractions and forwards wheel/drag gestures to .NET. All drawing
    // (geometry, scales, layout, animation) is done in C# and rendered as plain SVG.
    export class BitChart {
        // Observe an element's pixel size and report changes to .NET so the chart can render at real
        // device pixels (keeping font sizes constant, like Chart.js) instead of scaling a fixed viewBox.
        public static observe(element: HTMLElement, dotnet: DotNetObject, responsive: boolean) {
            let lastW = 0, lastH = 0;

            function report() {
                const r = element.getBoundingClientRect();
                const w = Math.round(r.width), h = Math.round(r.height);
                if (w > 0 && h > 0 && (Math.abs(w - lastW) > 1 || Math.abs(h - lastH) > 1)) {
                    lastW = w; lastH = h;
                    dotnet.invokeMethodAsync('OnResize', w, h);
                }
            }

            // Arrow/Home/End/Space navigate the focused chart. Blazor evaluates @onkeydown:preventDefault
            // at render time, so it cannot decide per key - and cancelling every keydown would break Tab.
            // A capture listener decides per key, and only while the SVG itself has focus.
            const NAV_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', ' ', 'Spacebar'];
            function onKeyDown(e: KeyboardEvent) {
                const target = e.target as Element | null;
                if (!target || target.tagName.toLowerCase() !== 'svg') return;
                if (NAV_KEYS.indexOf(e.key) >= 0) e.preventDefault();
            }
            element.addEventListener('keydown', onKeyDown, true);

            let ro: ResizeObserver | null = null;
            let listening = false;
            if (responsive) {
                if (typeof ResizeObserver !== 'undefined') {
                    ro = new ResizeObserver(report);
                    ro.observe(element);
                } else {
                    window.addEventListener('resize', report);
                    listening = true;
                }
                report();
            }

            return {
                dispose() {
                    element.removeEventListener('keydown', onKeyDown, true);
                    if (ro) ro.disconnect();
                    if (listening) window.removeEventListener('resize', report);
                }
            };
        }

        public static register(element: HTMLElement, dotnet: DotNetObject, opts: BitChartZoomOptions) {
            const state = { panning: false, lastX: 0, lastY: 0, startX: 0, startY: 0 };

            // Live touch/pen contacts, so a second finger can be recognised as a pinch.
            const contacts = new Map<number, { x: number, y: number }>();
            let pinchDistance = 0;

            // The browser's own pan/zoom would otherwise consume the gesture before it reaches us,
            // which is why a touch drag does nothing on a chart that has not opted out of it.
            const previousTouchAction = element.style.touchAction;
            if (opts.pan || opts.drag || opts.wheel) element.style.touchAction = 'none';

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

            function distance() {
                const points = Array.from(contacts.values());
                const dx = points[0].x - points[1].x;
                const dy = points[0].y - points[1].y;
                return Math.sqrt(dx * dx + dy * dy);
            }

            function onDown(e: PointerEvent) {
                if (e.button !== 0) return;
                // Contacts are tracked whatever the pan settings are: pinching is the touch equivalent
                // of the wheel, so it belongs to zoom rather than to panning.
                if (e.pointerType !== 'mouse' && (opts.wheel || opts.pan || opts.drag)) {
                    contacts.set(e.pointerId, { x: e.clientX, y: e.clientY });
                    if (contacts.size === 2) {
                        // A second finger turns the gesture into a pinch, so the pan stops here.
                        state.panning = false;
                        pinchDistance = distance();
                        return;
                    }
                }
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
                if (contacts.has(e.pointerId)) contacts.set(e.pointerId, { x: e.clientX, y: e.clientY });

                if (contacts.size === 2 && opts.wheel) {
                    const next = distance();
                    if (pinchDistance > 0 && next > 0) {
                        const points = Array.from(contacts.values());
                        const r = element.getBoundingClientRect();
                        const midX = (points[0].x + points[1].x) / 2;
                        const midY = (points[0].y + points[1].y) / 2;
                        dotnet.invokeMethodAsync('OnPinchZoom',
                            r.width ? (midX - r.left) / r.width : 0.5,
                            r.height ? (midY - r.top) / r.height : 0.5,
                            next / pinchDistance);
                    }
                    pinchDistance = next;
                    return;
                }

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
                contacts.delete(e.pointerId);
                if (contacts.size < 2) pinchDistance = 0;
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
            window.addEventListener('pointercancel', onUp);
            element.addEventListener('dblclick', onDouble);

            return {
                dispose() {
                    element.removeEventListener('wheel', onWheel);
                    element.removeEventListener('pointerdown', onDown);
                    element.removeEventListener('pointermove', onMove);
                    window.removeEventListener('pointerup', onUp);
                    window.removeEventListener('pointercancel', onUp);
                    element.removeEventListener('dblclick', onDouble);
                    element.style.touchAction = previousTouchAction;
                }
            };
        }

        // ---- export ----

        // Theme tokens the chart references from SVG attributes as var(--bit-...). They resolve against
        // the document, so an exported (standalone) SVG has to carry their computed values with it.
        private static readonly THEME_VARS = [
            '--bit-clr-fg-pri', '--bit-clr-fg-sec', '--bit-clr-brd-pri', '--bit-clr-brd-sec',
            '--bit-clr-bg-pri', '--bit-clr-pri', '--bit-tpg-font-family'
        ];

        private static serialize(element: HTMLElement, background: string | null): string | null {
            const svg = element.querySelector('svg') as SVGSVGElement | null;
            if (!svg) return null;

            const clone = svg.cloneNode(true) as SVGSVGElement;
            const box = svg.getBoundingClientRect();
            const width = Math.max(1, Math.round(box.width));
            const height = Math.max(1, Math.round(box.height));
            clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
            clone.setAttribute('width', String(width));
            clone.setAttribute('height', String(height));
            clone.style.overflow = 'visible';

            // Interaction-only layers are not part of the picture.
            clone.querySelectorAll('.bit-cht-hover, .bit-cht-bands').forEach(n => n.remove());

            const computed = getComputedStyle(svg);
            for (const name of BitChart.THEME_VARS) {
                const value = computed.getPropertyValue(name);
                if (value) clone.style.setProperty(name, value.trim());
            }
            if (background) {
                const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
                rect.setAttribute('width', '100%');
                rect.setAttribute('height', '100%');
                rect.setAttribute('fill', background);
                clone.insertBefore(rect, clone.firstChild);
            }
            return new XMLSerializer().serializeToString(clone);
        }

        public static exportSvg(element: HTMLElement, fileName: string, background: string | null) {
            const markup = BitChart.serialize(element, background);
            if (!markup) return false;
            BitChart.downloadBlob(fileName || 'chart.svg', new Blob([markup], { type: 'image/svg+xml;charset=utf-8' }));
            return true;
        }

        // Rasterizes the serialized SVG onto a canvas the caller can then read as a blob or a data URL.
        private static async rasterize(element: HTMLElement, scale: number, background: string | null) {
            const markup = BitChart.serialize(element, background);
            if (!markup) return null;
            const svg = element.querySelector('svg') as SVGSVGElement;
            const box = svg.getBoundingClientRect();
            const ratio = Math.max(1, scale || 1);
            const width = Math.max(1, Math.round(box.width * ratio));
            const height = Math.max(1, Math.round(box.height * ratio));

            const url = URL.createObjectURL(new Blob([markup], { type: 'image/svg+xml;charset=utf-8' }));
            try {
                const image = new Image();
                image.width = width;
                image.height = height;
                await new Promise<void>((resolve, reject) => {
                    image.onload = () => resolve();
                    image.onerror = () => reject(new Error('svg load failed'));
                    image.src = url;
                });
                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;
                const ctx = canvas.getContext('2d');
                if (!ctx) return null;
                if (background) {
                    ctx.fillStyle = background;
                    ctx.fillRect(0, 0, width, height);
                }
                ctx.drawImage(image, 0, 0, width, height);
                return canvas;
            } finally {
                URL.revokeObjectURL(url);
            }
        }

        public static async exportPng(element: HTMLElement, fileName: string, scale: number, background: string | null) {
            const canvas = await BitChart.rasterize(element, scale, background);
            if (!canvas) return false;
            const blob: Blob | null = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
            if (!blob) return false;
            BitChart.downloadBlob(fileName || 'chart.png', blob);
            return true;
        }

        // Returns the standalone SVG markup instead of downloading it, so the caller can embed,
        // upload or store the picture itself.
        public static toSvgString(element: HTMLElement, background: string | null) {
            return BitChart.serialize(element, background);
        }

        // Returns the rasterized chart as a data URL (the same picture exportPng downloads).
        public static async toDataUrl(element: HTMLElement, mimeType: string, scale: number, background: string | null) {
            const canvas = await BitChart.rasterize(element, scale, background);
            if (!canvas) return null;
            return canvas.toDataURL(mimeType || 'image/png');
        }

        public static downloadText(fileName: string, content: string, mimeType: string) {
            const type = mimeType || 'text/plain;charset=utf-8';
            // Spreadsheets read a CSV as the local codepage unless it opens with a byte order mark, so a
            // file of Japanese or Persian labels arrives as mojibake without one.
            const parts = type.indexOf('text/csv') === 0 ? ['﻿', content] : [content];
            BitChart.downloadBlob(fileName || 'chart.csv', new Blob(parts, { type }));
        }

        private static downloadBlob(fileName: string, blob: Blob) {
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName;
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            setTimeout(() => URL.revokeObjectURL(url), 0);
        }
    }
}
