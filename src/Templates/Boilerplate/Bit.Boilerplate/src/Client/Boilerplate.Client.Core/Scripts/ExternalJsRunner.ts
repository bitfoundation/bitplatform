// Checkout external-js-runner.html
class ExternalJsRunner {
    public static async run() {
        const host = window.origin.replace('http://', '');
        const localWebSocket = new WebSocket(`ws://${host}/external-js-runner`);
        localWebSocket.onmessage = async (event) => {
            const request = JSON.parse(event.data);
            let result = null;
            try {
                if (request.type == 'getCredential') {
                    result = await WebAuthn.getCredential(request.options);
                } else if (request.type == 'createCredential') {
                    result = await WebAuthn.createCredential(request.options);
                } else if (request.type == 'close') {
                    localWebSocket.close();
                    window.close();
                    window.location.assign('/close-browser');
                    return;
                }
                localWebSocket.send(JSON.stringify({ body: result }));
            }
            catch (err: any) {
                const errMsg = `${JSON.stringify(err, Object.getOwnPropertyNames(err))} ${err.toString()}`;
                localWebSocket.send(JSON.stringify({ error: errMsg }));
            }
        };
    }
}

(window as any).ExternalJsRunner = ExternalJsRunner;