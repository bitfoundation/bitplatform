const host = window as any;
const isPreview = location.pathname.startsWith('/preview');

const observedStages = new WeakSet<Element>();
const seenFrames = new WeakSet<Element>();

const resizeObserver = new ResizeObserver(entries => entries.forEach(entry => fitStage(entry.target as HTMLElement)));

let shellReference: any = null;
let previewReference: any = null;

function fitStage(stage: HTMLElement) {
    const scaler = stage.querySelector<HTMLElement>('.pane-scaler');
    const frameWidth = Number(stage.dataset.frameWidth);
    const frameHeight = Number(stage.dataset.frameHeight);

    if (!scaler || !frameWidth || !frameHeight) return;

    const zoom = stage.dataset.zoom;
    let scale = Number(zoom);

    if (!zoom || zoom === 'fit' || Number.isNaN(scale)) {
        const byHeight = stage.clientHeight / frameHeight;
        const byWidth = stage.dataset.fitWidth === 'true' ? stage.clientWidth / frameWidth : fitCanvasWidth(stage);
        scale = Math.min(1, byHeight, byWidth);
    }

    scale = Math.max(0.05, Math.floor(scale * 1000) / 1000);
    scaler.style.setProperty('--wb-scale', String(scale));

    const pane = stage.closest<HTMLElement>('.pane');
    if (pane) {
        pane.style.width = stage.dataset.fitWidth === 'true' ? '' : `${Math.ceil(frameWidth * scale) + 18}px`;
    }
}

function fitCanvasWidth(stage: HTMLElement) {
    const canvas = stage.closest<HTMLElement>('.canvas');
    if (!canvas) return Number.POSITIVE_INFINITY;

    const stages = Array.from(canvas.querySelectorAll<HTMLElement>('.pane-stage'));
    const totalFrameWidth = stages.reduce((sum, s) => sum + Number(s.dataset.frameWidth || 0), 0);
    const style = getComputedStyle(canvas);
    const chrome = stages.length * 20 + (stages.length - 1) * parseFloat(style.columnGap || '16') + parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);

    return totalFrameWidth > 0 ? Math.max(0.2, (canvas.clientWidth - chrome) / totalFrameWidth) : Number.POSITIVE_INFINITY;
}

function scan() {
    document.querySelectorAll<HTMLElement>('.pane-stage').forEach(stage => {
        if (!observedStages.has(stage)) {
            observedStages.add(stage);
            resizeObserver.observe(stage);
        }

        fitStage(stage);

        const frame = stage.querySelector('iframe');
        if (frame && !seenFrames.has(frame)) {
            seenFrames.add(frame);
            stage.classList.remove('ready');
        }
    });
}

function findFrame(paneId: string) {
    return document.querySelector<HTMLIFrameElement>(`iframe[data-pane-id="${CSS.escape(paneId)}"]`);
}

function frameOf(source: MessageEventSource | null) {
    return Array.from(document.querySelectorAll('iframe')).find(frame => frame.contentWindow === source);
}

function withTimeout<T>(promise: Promise<T>, milliseconds: number) {
    return Promise.race([promise, new Promise<null>(resolve => setTimeout(() => resolve(null), milliseconds))]);
}

if (!isPreview) {
    new MutationObserver(scan).observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['data-zoom', 'data-frame-width', 'data-frame-height', 'data-fit-width']
    });

    window.addEventListener('message', event => {
        if (event.origin !== location.origin) return;

        const frame = frameOf(event.source);
        if (!frame) return;

        if (event.data?.type === 'workbench-ready') {
            frame.closest('.pane-stage')?.classList.add('ready');
        }

        if (event.data?.type === 'workbench-scenario' && frame.dataset.paneId) {
            shellReference?.invokeMethodAsync('ReportScenario', frame.dataset.paneId, String(event.data.query ?? ''));
        }

        if (event.data?.type === 'workbench-page') {
            shellReference?.invokeMethodAsync('ReportPage', String(event.data.pageKey ?? ''));
        }
    });
}

host.workbench = {
    markReady() {
        host.__workbenchReady = true;
        document.documentElement.setAttribute('data-workbench-ready', '');

        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-ready' }, location.origin);
        }
    },

    registerShell(reference: any) {
        shellReference = reference;
    },

    registerPreview(reference: any) {
        previewReference = reference;
    },

    showInFrame(paneId: string, url: string) {
        const frame = findFrame(paneId);
        const target = new URL(url, location.origin).href;

        if (!frame || frame.contentWindow?.location.href === target) return;

        frame.closest('.pane-stage')?.classList.remove('ready');
        frame.contentWindow?.location.replace(target);
    },

    async captureFrame(paneId: string): Promise<string | null> {
        try {
            const preview = findFrame(paneId)?.contentWindow as any;
            return await withTimeout<string>(preview?.workbench?.captureState() ?? Promise.resolve(null), 4000);
        } catch {
            return null;
        }
    },

    async resetFrame(paneId: string) {
        try {
            const preview = findFrame(paneId)?.contentWindow as any;
            await withTimeout(preview?.workbench?.resetState() ?? Promise.resolve(), 4000);
        } catch {
        }
    },

    captureState(): Promise<string | null> {
        return previewReference ? previewReference.invokeMethodAsync('CaptureState') : Promise.resolve(null);
    },

    resetState() {
        return previewReference?.invokeMethodAsync('ResetState');
    },

    reportScenario(url: string, query: string, isApplied: boolean) {
        const hasShell = window.parent !== window;

        if (isApplied) {
            history.replaceState(history.state, '', url);
        } else if (!hasShell) {
            location.replace(url);
            return;
        }

        if (hasShell) {
            window.parent.postMessage({ type: 'workbench-scenario', query }, location.origin);
        }
    },

    reportPage(pageKey: string) {
        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-page', pageKey }, location.origin);
        }
    },

    download(url: string) {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = '';
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
    },

    copy(text: string) {
        return navigator.clipboard?.writeText(text);
    },

    scrollToEnd(element: HTMLElement) {
        element?.scrollTo({ top: element.scrollHeight, behavior: 'smooth' });
    },

    setCulture(name: string) {
        document.cookie = `.AspNetCore.Culture=${encodeURIComponent(`c=${name}|uic=${name}`)}; path=/; max-age=31536000; samesite=lax`;
        location.reload();
    }
};

const culture = isPreview ? document.documentElement.lang : null;

host.Blazor.start({
    circuit: {
        configureSignalR: (builder: any) => {
            if (culture) {
                builder.withUrl(`_blazor?culture=${encodeURIComponent(culture)}`);
            }
        }
    }
});
