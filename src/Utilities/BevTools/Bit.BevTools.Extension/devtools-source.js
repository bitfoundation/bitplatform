// The companion's source in the browser extension: the inspected page's BevTools hub (window.__BIT_BEVTOOLS__),
// reached through chrome.devtools.inspectedWindow.eval. eval returns synchronously computed, JSON-serializable values
// only, so a request starts in the page and its answer is collected from a mailbox on the page once it is ready.

const MAILBOX = "__BIT_BEVTOOLS_DEVTOOLS__";

/** A JavaScript string literal for any string: what goes into an evaluated expression is data, never code. */
function literal(value) {
    return JSON.stringify(String(value));
}

/**
 * @param {(expression: string) => Promise<any>} evalInPage evaluates an expression in the inspected page.
 * @param {(ms: number) => Promise<void>} [sleep]
 */
export function createDevToolsSource(evalInPage, sleep = ms => new Promise(resolve => setTimeout(resolve, ms))) {
    let next = 0;
    return {
        label: "the inspected page",
        live: true,

        async targets() {
            return (await evalInPage("(window.__BIT_BEVTOOLS__ ? window.__BIT_BEVTOOLS__.targets() : [])")) || [];
        },

        async traffic() {
            return (await evalInPage("(window.__BIT_BEVTOOLS__ ? window.__BIT_BEVTOOLS__.traffic() : [])")) || [];
        },

        async request(targetId, method, params) {
            const id = literal("r" + (++next) + "-" + Date.now());
            const args = params === undefined ? "undefined" : "JSON.parse(" + literal(JSON.stringify(params)) + ")";
            await evalInPage(
                "(function () {"
                + " var hub = window.__BIT_BEVTOOLS__;"
                + " if (!hub) throw new Error('No BevTools runtime on this page: it renders no <BevToolsPanel />, or BevTools is off.');"
                + " var box = window." + MAILBOX + " = window." + MAILBOX + " || {};"
                + " hub.request(" + literal(targetId) + ", " + literal(method) + ", " + args + ")"
                + "   .then(function (r) { box[" + id + "] = { r: JSON.stringify(r) }; }, function (e) { box[" + id + "] = { e: String(e && e.message || e) }; });"
                + " return true;"
                + "})()");

            // The answer is a JSON string, so a large snapshot crosses eval as one value instead of a deep object graph.
            for (let attempt = 0; attempt < 400; attempt++) {
                const answer = await evalInPage(
                    "(function () { var box = window." + MAILBOX + "; var x = box && box[" + id + "]; if (x) delete box[" + id + "]; return x || null; })()");
                if (answer) {
                    if (answer.e !== undefined) throw new Error(answer.e);
                    return JSON.parse(answer.r);
                }
                await sleep(Math.min(10 * (attempt + 1), 100));
            }
            throw new Error("The page did not answer " + method + " in time.");
        },
    };
}

/** inspectedWindow.eval as a promise, in Chromium and in Firefox (which also offers the chrome namespace). */
export function inspectedWindowEval(api) {
    return expression => new Promise((resolve, reject) => {
        api.devtools.inspectedWindow.eval(expression, (result, exceptionInfo) => {
            if (exceptionInfo && (exceptionInfo.isException || exceptionInfo.isError)) {
                reject(new Error(exceptionInfo.value || exceptionInfo.description || "The page could not be evaluated."));
            } else {
                resolve(result);
            }
        });
    });
}
