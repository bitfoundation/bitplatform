const query = new URLSearchParams(location.search);
const culture = query.get('culture');

(window as any).workbench = {
    markReady() {
        (window as any).__workbenchReady = true;
        document.documentElement.setAttribute('data-workbench-ready', '');
    }
};

(window as any).Blazor.start({
    circuit: {
        configureSignalR: (builder: any) => {
            if (culture) {
                builder.withUrl(`_blazor?culture=${encodeURIComponent(culture)}`);
            }
        }
    }
});
