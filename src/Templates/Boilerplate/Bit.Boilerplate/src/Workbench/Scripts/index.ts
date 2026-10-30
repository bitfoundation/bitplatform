const host = window as any;
const isPreview = location.pathname.startsWith('/preview');

const observedStages = new WeakSet<Element>();
const seenFrames = new WeakSet<Element>();

const resizeObserver = new ResizeObserver(entries => entries.forEach(entry => fitStage(entry.target as HTMLElement)));

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

if (!isPreview) {
    new MutationObserver(scan).observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['data-zoom', 'data-frame-width', 'data-frame-height', 'data-fit-width']
    });

    window.addEventListener('message', event => {
        if (event.origin !== location.origin || event.data?.type !== 'workbench-ready') return;

        document.querySelectorAll('iframe').forEach(frame => {
            if (frame.contentWindow === event.source) {
                frame.closest('.pane-stage')?.classList.add('ready');
            }
        });
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
