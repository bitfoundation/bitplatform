window.landing = {
    loadAssets(styles, scripts) {
        const load = (tag, attributes, selector) => document.querySelector(selector)
            ? Promise.resolve()
            : new Promise((resolve, reject) => {
                const element = Object.assign(document.createElement(tag), attributes, { onload: resolve, onerror: reject });
                (tag === 'link' ? document.head : document.body).appendChild(element);
            });

        return Promise.all([
            ...styles.map(href => load('link', { rel: 'stylesheet', href }, `link[href="${href}"]`)),
            ...scripts.map(src => load('script', { src }, `script[src="${src}"]`))
        ]);
    },

    setExpanded(selector, expanded) {
        const toggle = document.querySelector(selector);
        if (toggle && (toggle.getAttribute('aria-expanded') === 'true') !== expanded) {
            toggle.click();
        }
    },

    async press(selector, count, pause) {
        for (let pressed = 0; pressed < count; pressed++) {
            await new Promise(resolve => setTimeout(resolve, pause));
            const button = document.querySelector(selector);
            if (!button) return;
            button.click();
        }
    }
};
