class PubSub {
    private static allHandlers: { [key: string]: ((payload?: any) => Promise<unknown>)[] } = {};

    public static publish(message: string, payload?: any) {
        const handlers = PubSub.allHandlers[message];
        if (!handlers) return;

        const promises = handlers.map(h => {
            try {
                return h(payload);
            } catch (e) {
                return Promise.reject(e);
            }
        });

        try {
            Promise.allSettled(promises);
        } catch (e) {
            console.error(e);
        }
    }

    public static subscribe(message: string, handler: (payload?: any) => Promise<unknown>) {
        if (!PubSub.allHandlers[message]) {
            PubSub.allHandlers[message] = [];
        }

        PubSub.allHandlers[message].push(handler);
    }
}

(window as any).Ads = Ads;
