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
    }
};
