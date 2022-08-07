class BitSwiper {
    static getSwiperDimensions(swiper: HTMLElement) {
        const width = [].slice.call(swiper.children).reduce((pre, cur: HTMLDivElement) => pre + cur.offsetWidth, 0);
        const effectiveWidth = width - (swiper.parentElement?.offsetWidth ?? 0);

        const computedStyle = window.getComputedStyle(swiper);
        const matrix = computedStyle.getPropertyValue('transform');
        const matched = matrix.match(/matrix\((.+)\)/);

        let translateX = 0;
        if (matched && matched.length > 1) {
            const splitted = matched[1].split(',');
            translateX = +splitted[4];
        }

        return { width, effectiveWidth, translateX };
    }

    static registerPointerLeave(root: HTMLDivElement, dotnetObj: DotNetObject) {
        root.addEventListener('pointerleave', async e => {
            await dotnetObj.invokeMethodAsync("HandlePointerLeave", e.clientX);
        });
    }
}
