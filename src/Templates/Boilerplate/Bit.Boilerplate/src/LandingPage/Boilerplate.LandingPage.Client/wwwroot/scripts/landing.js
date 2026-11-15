window.landing = {
    // Each asset's download, so a caller asking while it's under way waits for the same one
    assets: new Map(),

    // Each once however often it's asked for, and a failed one again the next time
    loadAssets(styles, scripts) {
        const load = (tag, attributes, url, selector) => {
            const known = landing.assets.get(url);
            if (known) return known;

            // One the page itself carries
            if (document.querySelector(selector)) return Promise.resolve();

            const loading = new Promise((resolve, reject) => {
                const element = Object.assign(document.createElement(tag), attributes);

                element.onload = () => resolve();
                element.onerror = () => {
                    // Gone, so that asking again downloads it again rather than finding the element and taking it as loaded
                    element.remove();
                    landing.assets.delete(url);
                    reject(new Error(`Failed to load ${url}`));
                };

                (tag === 'link' ? document.head : document.body).appendChild(element);
            });

            landing.assets.set(url, loading);

            return loading;
        };

        return Promise.all([
            ...styles.map(href => load('link', { rel: 'stylesheet', href }, href, `link[href="${href}"]`)),
            ...scripts.map(src => load('script', { src }, src, `script[src="${src}"]`))
        ]);
    },

    // Clicks the toggle itself, so the component's own state changes, and only when it isn't already as asked
    setExpanded(selector, expanded) {
        const toggle = document.querySelector(selector);
        if (toggle && (toggle.getAttribute('aria-expanded') === 'true') !== expanded) {
            toggle.click();
        }
    },

    async press(selector, count, pause) {
        for (let pressed = 0; pressed < count; pressed++) {
            await new Promise(resolve => setTimeout(resolve, pause));
            // Looked up again for every press, as the one pressed last may be gone, like a checked-in guest's button
            const button = document.querySelector(selector);
            if (!button) return;
            button.click();
        }
    }
};
