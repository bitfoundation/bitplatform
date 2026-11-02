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

    /**
     * Runs an expression that yields a promise in the page, with the hub as `hub`, and collects its JSON answer from
     * the mailbox. `$0` - the Elements panel's selection - is in scope, as for every inspectedWindow.eval.
     */
    async function callAsync(expression, what) {
        const id = literal("r" + (++next) + "-" + Date.now());
        await evalInPage(
            "(function () {"
            + " var hub = window.__BIT_BEVTOOLS__;"
            + " if (!hub) throw new Error('No BevTools runtime on this page: it renders no <BevToolsPanel />, or BevTools is off.');"
            + " var box = window." + MAILBOX + " = window." + MAILBOX + " || {};"
            + " Promise.resolve(" + expression + ")"
            + "   .then(function (r) { box[" + id + "] = { r: JSON.stringify(r === undefined ? null : r) }; }, function (e) { box[" + id + "] = { e: String(e && e.message || e) }; });"
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
        throw new Error("The page did not answer " + what + " in time.");
    }

    /** A synchronous call on the hub's picking API; null on a page whose hub predates it. */
    const pageCall = expression => evalInPage("(function () { var hub = window.__BIT_BEVTOOLS__; return hub && typeof hub.startPick === 'function' ? " + expression + " : null; })()");

    return {
        label: "the inspected page",
        live: true,

        // Picking and outlining in the inspected page. The companion selects what was picked itself, so the in-app
        // panel is not told (it would open over the page).
        page: {
            startPick: async includeHidden => !!(await pageCall("hub.startPick(null, { includeHidden: " + (includeHidden ? "true" : "false") + ", tellPanel: false })")),
            stopPick: async () => { await pageCall("(hub.stopPick(), true)"); },
            isPicking: async () => !!(await pageCall("hub.isPicking()")),
            lastPick: async () => (await pageCall("hub.lastPick()")) || null,
            highlight: (targetId, instanceId, scroll) => callAsync(
                "hub.highlightComponent(" + literal(targetId) + ", " + Number(instanceId) + ", { scroll: " + (scroll ? "true" : "false") + ", durationMs: " + (scroll ? 2500 : 0) + " })", "highlight"),
            clearHighlight: async () => { await pageCall("(hub.clearHighlight(), true)"); },
            /** The component that rendered the node selected in the Elements panel. */
            inspectedElement: includeHidden => callAsync("typeof hub.componentAt === 'function' && $0 ? hub.componentAt($0, " + (includeHidden ? "true" : "false") + ") : null", "componentAt"),
        },

        async targets() {
            return (await evalInPage("(window.__BIT_BEVTOOLS__ ? window.__BIT_BEVTOOLS__.targets() : [])")) || [];
        },

        async traffic() {
            return (await evalInPage("(window.__BIT_BEVTOOLS__ ? window.__BIT_BEVTOOLS__.traffic() : [])")) || [];
        },

        request(targetId, method, params) {
            const args = params === undefined ? "undefined" : "JSON.parse(" + literal(JSON.stringify(params)) + ")";
            return callAsync("hub.request(" + literal(targetId) + ", " + literal(method) + ", " + args + ")", method);
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
