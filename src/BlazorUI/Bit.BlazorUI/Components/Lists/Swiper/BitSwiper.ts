namespace BitBlazorUI {
    export class Swiper {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static setup(
            id: string,
            element: HTMLDivElement,
            dotnetObj: DotNetObject) {

            const ac = new AbortController();
            Swiper._abortControllers[id] = ac;

            element.addEventListener('pointerleave', async e => {
                await dotnetObj.invokeMethodAsync("OnPointerLeave", e.clientX);
            }, { signal: ac.signal });
        }

        public static getDimensions(root: HTMLDivElement, container: HTMLDivElement) {
            if (!root) return {};

            const rootWidth = root.offsetWidth;
            const containerWidth = [].slice.call(container.children).reduce((pre, cur: HTMLDivElement) => pre + cur.offsetWidth, 0);
            const effectiveWidth = containerWidth - (container.parentElement?.offsetWidth ?? 0);

            const computedStyle = window.getComputedStyle(container);
            const matrix = computedStyle.getPropertyValue('transform');
            const matched = matrix.match(/matrix\((.+)\)/);

            let translateX = 0;
            if (matched && matched.length > 1) {
                const splitted = matched[1].split(',');
                translateX = +splitted[4];
            }

            return {
                rootWidth,
                containerWidth,
                effectiveWidth,
                translateX
            };
        }

        public static dispose(id: string) {
            const ac = Swiper._abortControllers[id];
            if (!ac) return;

            ac.abort();
            delete Swiper._abortControllers[id];
        }
    }
}
