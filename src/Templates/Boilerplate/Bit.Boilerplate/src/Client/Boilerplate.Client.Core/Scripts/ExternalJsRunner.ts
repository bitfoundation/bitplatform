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
                    result = {};
                    setTimeout(() => {
                        localWebSocket.close();
                        window.close();
                    }, 100);
                }
                localWebSocket.send(JSON.stringify({ body: result }));
            }
            catch (e: any) {
                let errMsg;
                if (e instanceof Error) {
                    errMsg = `${e.message} ${e.name} ${e.cause} ${e.stack}`;
                }
                else {
                    errMsg = e?.toString();
                }
                localWebSocket.send(JSON.stringify({ error: errMsg }));
            }
        };
    }
}

(window as any).ExternalJsRunner = ExternalJsRunner;