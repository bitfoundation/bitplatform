namespace BitBlazorUI {
    interface BitImageViewerOptions {
        min: number;
        max: number;
        factor: number;
        doubleClick: number;
        wheel: boolean;
        disabled: boolean;
    }

    // The zoom and pan of a BitImageViewer. Every gesture is answered here, on the frame the browser paints, so dragging
    // and pinching follow the pointer without a round trip; .NET is told the zoom the gesture ends at (OnZoom), which is
    // what its Zoom parameter and toolbar show, and tells this side the zoom it sets (setZoom).
    //
    // The image is laid out fitted in the viewport by the stylesheet (max-width and max-height of 100%, centered), which
    // is zoom 1; zooming scales that box from its center and panning moves it, both through one transform. The pan is
    // kept so the image never leaves a gap at an edge it could cover: an image narrower than the viewport stays centered
    // on that axis.
    export class BitImageViewer {
        public static setup(viewport: HTMLElement, dotnet: DotNetObject, options: BitImageViewerOptions, zoom: number) {
            const img = viewport.querySelector('img') as HTMLImageElement | null;

            let opts = options;
            let z = zoom;
            let tx = 0, ty = 0;
            let lastSrc = img?.currentSrc || img?.src || '';

            // What .NET was last told, and the report waiting for the one in flight to finish, so a pinch that changes
            // the zoom every frame sends one call at a time and always ends on the zoom it stopped at.
            let reported = zoom;
            let sending = false;
            let pending: number | null = null;

            const pointers = new Map<number, { x: number, y: number }>();

            // A pinch keeps the point of the image that was under the middle of the two fingers as it started under
            // their middle now - it zooms by how far apart they are, and pans by where they are - rather than zooming
            // around a middle that every move of one finger before the other would shift, and drift by.
            let pinch: { distance: number, zoom: number, imageX: number, imageY: number } | null = null;

            function round(value: number) {
                return Math.round(value * 10000) / 10000;
            }

            function clampZoom(value: number) {
                const max = Math.max(opts.min, opts.max);
                return Math.min(max, Math.max(opts.min, value));
            }

            function canPan() {
                if (!img) return false;
                return img.offsetWidth * z > viewport.clientWidth + 0.5 || img.offsetHeight * z > viewport.clientHeight + 0.5;
            }

            function clampPan() {
                if (!img) return;
                const maxX = Math.max(0, (img.offsetWidth * z - viewport.clientWidth) / 2);
                const maxY = Math.max(0, (img.offsetHeight * z - viewport.clientHeight) / 2);
                tx = Math.min(maxX, Math.max(-maxX, tx));
                ty = Math.min(maxY, Math.max(-maxY, ty));
            }

            function apply() {
                if (!img) return;
                clampPan();
                img.style.transform = `translate(${tx}px, ${ty}px) scale(${z})`;
                viewport.classList.toggle('bit-imv-pan', canPan());
            }

            function send(value: number) {
                sending = true;
                dotnet.invokeMethodAsync('OnZoom', value).catch(() => { }).finally(() => {
                    sending = false;
                    if (pending !== null) {
                        const next = pending;
                        pending = null;
                        if (next !== reported) {
                            reported = next;
                            send(next);
                        }
                    }
                });
            }

            function report() {
                const value = round(z);
                if (sending) {
                    pending = value;
                    return;
                }
                if (value === reported) return;
                reported = value;
                send(value);
            }

            // The point of the viewport (from its center) the zoom is anchored at, which stays where it is on the
            // screen: the pointer for the wheel, a pinch and a double-click, the center for the keys and the buttons.
            function point(clientX: number, clientY: number) {
                const r = viewport.getBoundingClientRect();
                return { x: clientX - r.left - r.width / 2, y: clientY - r.top - r.height / 2 };
            }

            function zoomTo(value: number, at = { x: 0, y: 0 }) {
                const next = clampZoom(value);
                if (next === z) return;
                tx = at.x - next * (at.x - tx) / z;
                ty = at.y - next * (at.y - ty) / z;
                z = next;
                apply();
                report();
            }

            function onWheel(e: WheelEvent) {
                if (opts.disabled) return;
                // A plain wheel scrolls the page the viewer sits in, unless the viewer was asked to take it: Ctrl (or
                // Cmd), and a trackpad's pinch, which the browsers report as a wheel with Ctrl held, zoom it.
                if (!(e.ctrlKey || e.metaKey || opts.wheel)) return;
                e.preventDefault();
                const delta = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaMode === 2 ? e.deltaY * viewport.clientHeight : e.deltaY;
                zoomTo(z * Math.exp(-delta * 0.002), point(e.clientX, e.clientY));
            }

            function distance() {
                const [a, b] = Array.from(pointers.values());
                return Math.hypot(a.x - b.x, a.y - b.y);
            }

            function middle() {
                const [a, b] = Array.from(pointers.values());
                return point((a.x + b.x) / 2, (a.y + b.y) / 2);
            }

            function onPointerDown(e: PointerEvent) {
                if (opts.disabled) return;
                if (e.pointerType === 'mouse' && e.button !== 0) return;
                pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
                try { viewport.setPointerCapture(e.pointerId); } catch { }
                if (pointers.size === 2) {
                    const at = middle();
                    pinch = { distance: distance(), zoom: z, imageX: (at.x - tx) / z, imageY: (at.y - ty) / z };
                }
            }

            function onPointerMove(e: PointerEvent) {
                const last = pointers.get(e.pointerId);
                if (!last) return;
                const dx = e.clientX - last.x, dy = e.clientY - last.y;
                pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

                if (pointers.size === 2 && pinch) {
                    if (pinch.distance > 0) {
                        const at = middle();
                        z = clampZoom(pinch.zoom * distance() / pinch.distance);
                        tx = at.x - z * pinch.imageX;
                        ty = at.y - z * pinch.imageY;
                        apply();
                        report();
                    }
                    return;
                }

                if (pointers.size === 1 && canPan()) {
                    viewport.classList.add('bit-imv-drg');
                    tx += dx;
                    ty += dy;
                    apply();
                }
            }

            function onPointerUp(e: PointerEvent) {
                if (!pointers.delete(e.pointerId)) return;
                try { viewport.releasePointerCapture(e.pointerId); } catch { }
                if (pointers.size < 2) pinch = null;
                if (pointers.size === 0) viewport.classList.remove('bit-imv-drg');
            }

            function onDoubleClick(e: MouseEvent) {
                if (opts.disabled) return;
                e.preventDefault();
                const at = point(e.clientX, e.clientY);
                zoomTo(z > 1.001 ? 1 : opts.doubleClick, at);
            }

            function onKeyDown(e: KeyboardEvent) {
                if (opts.disabled || e.target !== viewport || e.altKey || e.ctrlKey || e.metaKey) return;

                switch (e.key) {
                    case '+':
                    case '=':
                        zoomTo(z * opts.factor);
                        break;
                    case '-':
                    case '_':
                        zoomTo(z / opts.factor);
                        break;
                    case '0':
                        reset();
                        break;
                    case 'ArrowLeft':
                    case 'ArrowRight':
                    case 'ArrowUp':
                    case 'ArrowDown': {
                        // An image that fits has nowhere to go, and leaves the keys to the page.
                        if (!canPan()) return;
                        const stepX = viewport.clientWidth * 0.1, stepY = viewport.clientHeight * 0.1;
                        if (e.key === 'ArrowLeft') tx += stepX;
                        if (e.key === 'ArrowRight') tx -= stepX;
                        if (e.key === 'ArrowUp') ty += stepY;
                        if (e.key === 'ArrowDown') ty -= stepY;
                        apply();
                        break;
                    }
                    default:
                        return;
                }

                e.preventDefault();
            }

            function reset() {
                tx = 0;
                ty = 0;
                z = clampZoom(1);
                apply();
                report();
            }

            // A new picture starts fitted again; the first one keeps the zoom it was given, now that it can be measured.
            function onLoad() {
                const src = img?.currentSrc || img?.src || '';
                if (src !== lastSrc) {
                    lastSrc = src;
                    reset();
                } else {
                    apply();
                }
            }

            const resizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => apply());
            resizeObserver?.observe(viewport);

            viewport.addEventListener('wheel', onWheel, { passive: false });
            viewport.addEventListener('pointerdown', onPointerDown);
            viewport.addEventListener('pointermove', onPointerMove);
            viewport.addEventListener('pointerup', onPointerUp);
            viewport.addEventListener('pointercancel', onPointerUp);
            viewport.addEventListener('dblclick', onDoubleClick);
            viewport.addEventListener('keydown', onKeyDown);
            img?.addEventListener('load', onLoad);

            z = clampZoom(z);
            apply();

            return {
                setZoom(value: number) {
                    reported = round(clampZoom(value));
                    zoomTo(value);
                    if (round(z) !== reported) report();
                },
                update(next: BitImageViewerOptions) {
                    opts = next;
                    const clamped = clampZoom(z);
                    if (clamped !== z) {
                        zoomTo(clamped);
                    } else {
                        apply();
                    }
                },
                dispose() {
                    resizeObserver?.disconnect();
                    viewport.removeEventListener('wheel', onWheel);
                    viewport.removeEventListener('pointerdown', onPointerDown);
                    viewport.removeEventListener('pointermove', onPointerMove);
                    viewport.removeEventListener('pointerup', onPointerUp);
                    viewport.removeEventListener('pointercancel', onPointerUp);
                    viewport.removeEventListener('dblclick', onDoubleClick);
                    viewport.removeEventListener('keydown', onKeyDown);
                    img?.removeEventListener('load', onLoad);
                }
            };
        }
    }
}
