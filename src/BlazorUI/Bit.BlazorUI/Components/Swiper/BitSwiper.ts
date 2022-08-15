class BitSwiper {
    static getDimensions(root: HTMLDivElement, swiper: HTMLDivElement) {
        const swiperWidth = [].slice.call(swiper.children).reduce((pre, cur: HTMLDivElement) => pre + cur.offsetWidth, 0);
        const effectiveSwiperWidth = swiperWidth - (swiper.parentElement?.offsetWidth ?? 0);

        const computedStyle = window.getComputedStyle(swiper);
        const matrix = computedStyle.getPropertyValue('transform');
        const matched = matrix.match(/matrix\((.+)\)/);

        let swiperTranslateX = 0;
        if (matched && matched.length > 1) {
            const splitted = matched[1].split(',');
            swiperTranslateX = +splitted[4];
        }

        return { rootWidth: root.offsetWidth, swiperWidth, effectiveSwiperWidth, swiperTranslateX };
    }

    static registerPointerLeave(root: HTMLDivElement, dotnetObj: DotNetObject) {
        root.addEventListener('pointerleave', async e => {
            await dotnetObj.invokeMethodAsync("HandlePointerLeave", e.clientX);
        });
    }
}
