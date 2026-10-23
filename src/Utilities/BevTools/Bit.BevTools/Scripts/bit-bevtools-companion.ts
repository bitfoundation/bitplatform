// bit BevTools companion: a frontend that runs outside the inspected page - in a window of its own, or in the browser
// extension's DevTools panel - and shows every BevTools runtime of that page through the protocol the runtimes answer
// in .NET (BevToolsProtocol). Opened without a page, it reads an exported session (bit-bevtools/1) instead.
//
// Everything application-provided (component names, titles, messages, values) is rendered as text nodes, never as
// markup, so nothing the inspected app produces can execute here.

export interface CompanionTarget {
    id: string;
    sessionId: number | null;
    platform: string | null;
    renderMode: string | null;
    label: string;
}

/** Where the companion gets its data: the page hub (window, extension) or an exported file. */
export interface CompanionSource {
    readonly label: string;
    readonly live: boolean;
    /** Components in the inspected page: picking and outlining. Absent for a file, or a page whose hub predates it. */
    readonly page?: CompanionPage | null;
    targets(): Promise<CompanionTarget[]>;
    request(targetId: string, method: string, params?: any): Promise<any>;
    traffic(): Promise<any[]>;
}

/** The page hub's picking and outlining, as a frontend outside the page reaches it. */
export interface CompanionPage {
    startPick(includeHidden: boolean): Promise<boolean>;
    stopPick(): Promise<void>;
    isPicking(): Promise<boolean>;
    lastPick(): Promise<{ targetId?: string; instanceId?: number; name?: string; error?: string; at: number } | null>;
    highlight(targetId: string, instanceId: number, scroll: boolean): Promise<{ ok: boolean; visible?: boolean; error?: string }>;
    clearHighlight(): Promise<void>;
    /** The component that rendered the node selected in the browser's Elements panel (the extension only). */
    inspectedElement?(includeHidden: boolean): Promise<{ targetId?: string; instanceId?: number; error?: string } | null>;
}

type Doc = any;

const POLL_MS = 1500;
const MAX_EVENTS = 5000;
const MAX_ROWS = 600;

// ----- DOM helpers -----

type Child = Node | string | number | null | undefined | false | Child[];

function h(tag: string, attrs?: { [key: string]: any } | null, ...children: Child[]): HTMLElement {
    const el = document.createElement(tag);
    if (attrs) {
        for (const key of Object.keys(attrs)) {
            const value = attrs[key];
            if (value === undefined || value === null || value === false) continue;
            if (key.startsWith("on") && typeof value === "function") el.addEventListener(key.slice(2), value);
            else if (key === "class") el.className = String(value);
            else if (key === "style") el.setAttribute("style", String(value));
            else el.setAttribute(key, value === true ? "" : String(value));
        }
    }
    append(el, children);
    return el;
}

function append(el: Node, children: Child[]) {
    for (const child of children) {
        if (child === null || child === undefined || child === false) continue;
        if (Array.isArray(child)) append(el, child);
        else if (child instanceof Node) el.appendChild(child);
        else el.appendChild(document.createTextNode(String(child)));
    }
}

const fmt = {
    ms(v: number | null | undefined) {
        if (v === null || v === undefined || !isFinite(v)) return "–";
        if (v < 10) return v.toFixed(2) + " ms";
        if (v < 1000) return v.toFixed(1) + " ms";
        return (v / 1000).toFixed(2) + " s";
    },
    bytes(v: number | null | undefined) {
        if (v === null || v === undefined) return "–";
        if (v < 1024) return v + " B";
        if (v < 1024 * 1024) return (v / 1024).toFixed(1) + " KB";
        return (v / 1024 / 1024).toFixed(2) + " MB";
    },
    time(iso: string | null | undefined) {
        if (!iso) return "–";
        const d = new Date(iso);
        return isNaN(+d) ? String(iso) : d.toLocaleTimeString([], { hour12: false }) + "." + String(d.getMilliseconds()).padStart(3, "0");
    },
    path(url: string) {
        const i = url.indexOf("://");
        if (i < 0) return url;
        const slash = url.indexOf("/", i + 3);
        return slash < 0 ? "/" : url.slice(slash);
    },
    // "GET https://shop.test/api/orders/{id}" → "GET /api/orders/{id}", as the in-app panel shows it.
    endpoint(endpoint: string) {
        const space = endpoint.indexOf(" ");
        return space > 0 ? endpoint.slice(0, space + 1) + fmt.path(endpoint.slice(space + 1)) : endpoint;
    },
};

function matches(filter: string, ...fields: (string | null | undefined)[]) {
    const terms = filter.toLowerCase().split(/\s+/).filter(Boolean);
    return terms.every(t => fields.some(f => f && f.toLowerCase().includes(t)));
}

function download(fileName: string, content: string, contentType: string) {
    const url = URL.createObjectURL(new Blob([content], { type: contentType }));
    const a = h("a", { href: url, download: fileName }) as HTMLAnchorElement;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { a.remove(); URL.revokeObjectURL(url); }, 0);
}

// ----- sources -----

/** The page that opened this window: the hub it exposes, read afresh on every call so a reload of the page is survived. */
export class OpenerSource implements CompanionSource {
    readonly label = "this window's opener";
    readonly live = true;

    private hook(): any {
        const opener = window.opener;
        if (!opener || opener.closed) throw new Error("The page that opened this window is gone.");
        const hook = opener.__BIT_BEVTOOLS__;
        if (!hook) throw new Error("No BevTools runtime on the page (yet): open a page that renders <BevToolsPanel />.");
        return hook;
    }

    async targets() { return this.hook().targets(); }

    request(targetId: string, method: string, params?: any) { return this.hook().request(targetId, method, params); }

    async traffic() { return this.hook().traffic(); }

    get page(): CompanionPage | null {
        let hook: any;
        try { hook = this.hook(); } catch { return null; }
        if (typeof hook.startPick !== "function") return null;
        return {
            // The companion selects what was picked itself; the in-app panel must not open over the page for it.
            startPick: async includeHidden => this.hook().startPick(null, { includeHidden, tellPanel: false }),
            stopPick: async () => this.hook().stopPick(),
            isPicking: async () => this.hook().isPicking(),
            lastPick: async () => this.hook().lastPick(),
            highlight: (targetId, instanceId, scroll) => this.hook().highlightComponent(targetId, instanceId, { scroll, durationMs: scroll ? 2500 : 0 }),
            clearHighlight: async () => this.hook().clearHighlight(),
        };
    }
}

/** Nothing to show yet: the companion was opened on its own. */
export class EmptySource implements CompanionSource {
    readonly label = "no file";
    readonly live = false;

    async targets(): Promise<CompanionTarget[]> { return []; }

    async request(): Promise<any> { return { error: "Nothing is open." }; }

    async traffic() { return []; }
}

/** An exported session: one runtime, as it was when exported, read only. */
export class FileSource implements CompanionSource {
    readonly live = false;
    readonly label: string;

    constructor(private readonly doc: Doc, fileName: string) {
        this.label = fileName;
    }

    async targets(): Promise<CompanionTarget[]> {
        const s = this.doc.session || {};
        return [{ id: "file", sessionId: s.id ?? null, platform: s.platform ?? null, renderMode: s.renderMode ?? null, label: "Exported session #" + (s.id ?? "?") }];
    }

    async request(_targetId: string, method: string): Promise<any> {
        switch (method) {
            case "hello": return { protocol: 1, sessionId: this.doc.session?.id, platform: this.doc.session?.platform, valueAccess: this.doc.session?.valueAccess, capturing: false };
            case "snapshot": return { version: 0, session: this.doc };
            default: return { error: "An exported session cannot " + method + ": open the live page for that." };
        }
    }

    async traffic() { return (this.doc.browser && this.doc.browser.sockets) || []; }
}

// ----- per-runtime model -----

class TargetModel {
    doc: Doc | null = null;
    version = -1;
    events: any[] = [];
    lastEventId = 0;
    hello: any = null;
    error: string | null = null;
    inspected: any = null;
    inspectedId: number | null = null;
    expanded = new Map<string, any[]>();

    constructor(public info: CompanionTarget) { }

    merge(snapshot: any) {
        if (!snapshot || snapshot.unchanged) return false;
        const doc = snapshot.session;
        this.version = snapshot.version;
        const incoming: any[] = doc.timeline || [];
        if (this.lastEventId === 0) {
            this.events = incoming.slice();
        } else {
            const byId = new Map<number, any>();
            for (const e of this.events) byId.set(e.id, e);
            for (const e of incoming) byId.set(e.id, e);
            this.events = Array.from(byId.values()).sort((a, b) => a.id - b.id);
        }
        if (this.events.length > MAX_EVENTS) this.events = this.events.slice(this.events.length - MAX_EVENTS);
        if (this.events.length) this.lastEventId = this.events[this.events.length - 1].id;
        doc.timeline = this.events;
        this.doc = doc;
        return true;
    }
}

// ----- the app -----

interface ViewState {
    filter: string;
    selected: any;
    flag: boolean;
    sort: string;
}

type Tab = { id: string; title: string; badge?: (m: TargetModel) => string | null };

const TABS: Tab[] = [
    { id: "overview", title: "Overview" },
    { id: "components", title: "Components" },
    { id: "profiler", title: "Profiler" },
    { id: "timeline", title: "Timeline" },
    { id: "state", title: "State" },
    { id: "forms", title: "Forms" },
    { id: "routing", title: "Routing" },
    { id: "network", title: "Network", badge: m => count(m.doc?.http?.filter((r: any) => r.failed)) },
    { id: "interop", title: "JS Interop" },
    { id: "errors", title: "Errors", badge: m => count(m.doc?.errors) },
    { id: "diagnostics", title: "Diagnostics", badge: m => count(m.doc?.diagnostics?.filter((f: any) => !f.dismissed && f.severity !== "Info")) },
    { id: "runtime", title: "Runtime" },
];

function count(list: any[] | undefined) {
    return list && list.length ? String(list.length) : null;
}

export class Companion {
    private targets: TargetModel[] = [];
    private active: TargetModel | null = null;
    private tab = "overview";
    private views = new Map<string, ViewState>();
    private sockets: any[] = [];
    private timer: ReturnType<typeof setInterval> | null = null;
    private busy = false;
    private polls = 0;
    private notice: string | null = null;
    private pickTimer: ReturnType<typeof setInterval> | null = null;
    private readonly root: HTMLElement;
    private header!: HTMLElement;
    private toolbar!: HTMLElement;
    private content!: HTMLElement;
    private status!: HTMLElement;

    constructor(private source: CompanionSource, root?: HTMLElement) {
        this.root = root || document.body;
    }

    async start() {
        this.layout();
        await this.refreshTargets();
        await this.poll(true);
        this.timer = setInterval(() => { if (!document.hidden) this.poll(false); }, POLL_MS);
    }

    stop() {
        if (this.timer) clearInterval(this.timer);
        this.timer = null;
        this.endPick(true);
    }

    use(source: CompanionSource) {
        this.stop();
        this.source = source;
        this.targets = [];
        this.active = null;
        this.views.clear();
        this.start();
    }

    private view(id = this.tab): ViewState {
        let v = this.views.get(id);
        if (!v) this.views.set(id, v = { filter: "", selected: null, flag: false, sort: "" });
        return v;
    }

    private layout() {
        this.header = h("header", { class: "bc-header" });
        this.toolbar = h("div", { class: "bc-toolbar" });
        this.content = h("main", { class: "bc-content" });
        this.status = h("footer", { class: "bc-status" });
        this.root.replaceChildren(this.header, this.toolbar, this.content, this.status);
        this.root.addEventListener("dragover", e => { e.preventDefault(); });
        this.root.addEventListener("drop", e => {
            e.preventDefault();
            const file = e.dataTransfer && e.dataTransfer.files[0];
            if (file) this.openFile(file);
        });
    }

    private async refreshTargets() {
        try {
            const list: CompanionTarget[] = await this.source.targets();
            const known = new Map(this.targets.map(t => [t.info.id, t]));
            this.targets = list.map(info => {
                const existing = known.get(info.id);
                if (existing) { existing.info = info; return existing; }
                return new TargetModel(info);
            });
            if (!this.active || !this.targets.includes(this.active)) this.active = this.targets[0] || null;
            this.notice = this.targets.length ? null : this.source.live
                ? "No BevTools runtime on the page yet."
                : "Open an exported session (📂 Open file, or drop it here), or open this window from a BevTools panel (⧉) to inspect a live page.";
        } catch (err: any) {
            this.notice = String(err && err.message || err);
        }
        this.renderHeader();
    }

    private async poll(force: boolean) {
        if (this.busy) return;
        this.busy = true;
        let changed = force;
        try {
            // The runtimes of a page come and go (a circuit closing, WebAssembly taking over in Auto); every few polls.
            if (this.source.live && this.polls++ % 3 === 0) await this.refreshTargets();
            const t = this.active;
            if (t) {
                try {
                    t.hello ??= await this.source.request(t.info.id, "hello");
                    const answer = await this.source.request(t.info.id, "snapshot", t.version >= 0 ? { since: t.version, timelineAfter: t.lastEventId } : {});
                    if (answer && answer.error) t.error = answer.error;
                    else { t.error = null; changed = t.merge(answer) || changed; }
                } catch (err: any) {
                    t.error = String(err && err.message || err);
                    changed = true;
                }
            }
            try {
                const sockets = await this.source.traffic();
                if (JSON.stringify(sockets) !== JSON.stringify(this.sockets)) {
                    this.sockets = sockets;
                    changed = changed || this.tab === "network";
                }
            } catch { /* the hub is gone; the targets refresh says so */ }
        } finally {
            this.busy = false;
        }
        if (changed) {
            this.renderHeader();
            this.renderContent();
        }
        this.renderStatus();
    }

    private async select(target: TargetModel) {
        this.active = target;
        this.renderHeader();
        this.renderToolbar();
        await this.poll(true);
    }

    private showTab(id: string) {
        this.tab = id;
        this.renderHeader();
        this.renderToolbar();
        this.renderContent();
    }

    // ----- header, toolbar, status -----

    private renderHeader() {
        const t = this.active;
        const runtimes = this.targets.map(target => h("button", {
            class: "bc-runtime" + (target === t ? " bc-active" : ""),
            title: target.info.label,
            onclick: () => this.select(target),
        }, target.info.platform || "Blazor", h("span", { class: "bc-muted" }, " #" + (target.info.sessionId ?? "?"))));

        const capturing = t?.doc?.session?.capturing !== false;
        const actions = this.source.live && t ? [
            h("button", { title: capturing ? "Pause capture in this runtime" : "Resume capture", onclick: () => this.command(capturing ? "pause" : "resume") }, capturing ? "⏸ Pause" : "▶ Resume"),
            h("button", { title: "Reset the profiler baseline", onclick: () => this.command("baseline") }, "⟲ Baseline"),
            h("button", { title: "Clear captured event histories", onclick: () => this.command("clear") }, "🗑 Clear"),
        ] : [];
        const exports = t ? [
            h("button", { title: "Download the session as bit-bevtools/1 JSON", onclick: () => this.export("json") }, "⬇ Session"),
            this.source.live ? h("button", { title: "Download a performance trace for ui.perfetto.dev or Chrome DevTools", onclick: () => this.export("trace") }, "⬇ Trace") : null,
            this.source.live ? h("button", { title: "Download a Markdown report", onclick: () => this.export("markdown") }, "⬇ Report") : null,
        ] : [];
        const fileInput = h("input", { type: "file", accept: ".json,application/json", style: "display:none", onchange: (e: Event) => { const f = (e.target as HTMLInputElement).files?.[0]; if (f) this.openFile(f); } }) as HTMLInputElement;

        const tabs = TABS.map(tab => {
            const badge = t && tab.badge ? tab.badge(t) : null;
            return h("button", { class: "bc-tab" + (tab.id === this.tab ? " bc-active" : ""), role: "tab", "aria-selected": tab.id === this.tab ? "true" : "false", onclick: () => this.showTab(tab.id) },
                tab.title, badge ? h("span", { class: "bc-count" }, badge) : null);
        });

        this.header.replaceChildren(
            h("div", { class: "bc-bar" },
                h("strong", { class: "bc-title" }, "BevTools"),
                h("span", { class: "bc-runtimes", role: "group", "aria-label": "Runtimes" }, runtimes),
                h("span", { class: "bc-spacer" }),
                actions,
                exports,
                h("button", { title: "Open an exported session (bit-bevtools/1 JSON); you can also drop it on this window", onclick: () => fileInput.click() }, "📂 Open file"),
                fileInput),
            h("nav", { class: "bc-tabs", role: "tablist" }, tabs));
        if (!this.toolbar.childNodes.length) this.renderToolbar();
    }

    private renderStatus() {
        const t = this.active;
        const parts: Child[] = [this.source.live ? h("span", { class: "bc-live" }, "● live") : h("span", { class: "bc-muted" }, "file: " + this.source.label)];
        if (t?.doc) {
            const s = t.doc.session || {};
            parts.push(" · ", (s.platform || "?") + (s.renderMode ? " (" + s.renderMode + ")" : ""), " · session #" + s.id, " · value access " + (s.valueAccess || "?"));
            if (s.capturing === false) parts.push(" · ", h("span", { class: "bc-warn" }, "capture paused"));
        }
        if (t?.error) parts.push(" · ", h("span", { class: "bc-err" }, t.error));
        if (this.notice) parts.push(" · ", h("span", { class: "bc-warn" }, this.notice));
        this.status.replaceChildren(...parts.map(p => typeof p === "string" ? document.createTextNode(p) : p as Node));
    }

    private renderToolbar() {
        const v = this.view();
        const placeholders: { [id: string]: string } = {
            components: "Filter components…",
            profiler: "Filter components…",
            timeline: "Filter events (text, kind)…",
            network: "Filter requests…",
            interop: "Filter identifiers…",
            errors: "Filter errors…",
            routing: "Filter routes…",
            state: "Filter providers…",
        };
        const placeholder = placeholders[this.tab];
        if (!placeholder) {
            this.toolbar.replaceChildren();
            this.toolbar.hidden = true;
            return;
        }
        this.toolbar.hidden = false;
        const input = h("input", { type: "search", placeholder, value: v.filter, oninput: (e: Event) => { v.filter = (e.target as HTMLInputElement).value; this.renderContent(); } });
        const extra: Child[] = [];
        if (this.tab === "profiler") {
            extra.push(h("label", null, h("input", { type: "checkbox", checked: v.flag, onchange: (e: Event) => { v.flag = (e.target as HTMLInputElement).checked; this.renderContent(); } }), " by type"));
        }
        if (this.tab === "components") {
            extra.push(h("label", { title: "Framework and library components hidden by default" }, h("input", { type: "checkbox", checked: v.flag, onchange: (e: Event) => { v.flag = (e.target as HTMLInputElement).checked; this.renderContent(); } }), " internals"));
            if (this.source.live && this.source.page) {
                extra.push(h("button", {
                    class: this.pickTimer ? "bc-active" : "",
                    "aria-pressed": this.pickTimer ? "true" : "false",
                    title: "Pick a component in the page: hover to outline it, click to inspect it (Esc in the page cancels)",
                    onclick: () => this.togglePick(),
                }, "⌖ Pick"));
            }
        }
        this.toolbar.replaceChildren(input, ...extra.map(x => x as Node));
    }

    private renderContent() {
        const scroller = this.content.querySelector(".bc-scroll") as HTMLElement | null;
        const scroll = scroller ? scroller.scrollTop : 0;
        const t = this.active;
        let body: Node;
        if (!t) {
            body = this.empty(this.notice || "Waiting for a BevTools runtime…", this.source.live ? "Open a page of the app that renders <BevToolsPanel />. In a Blazor Web App every runtime of the page (a circuit, WebAssembly) appears up here." : null);
        } else if (!t.doc) {
            body = this.empty(t.error || "Reading " + t.info.label + "…", null);
        } else {
            try {
                body = this.renderTab(t);
            } catch (err: any) {
                body = this.empty("This view failed to render: " + String(err && err.message || err), null);
            }
        }
        this.content.replaceChildren(body);
        const next = this.content.querySelector(".bc-scroll") as HTMLElement | null;
        if (next) next.scrollTop = scroll;
    }

    private renderTab(t: TargetModel): Node {
        switch (this.tab) {
            case "components": return this.components(t);
            case "profiler": return this.profiler(t);
            case "timeline": return this.timeline(t);
            case "state": return this.state(t);
            case "forms": return this.forms(t);
            case "routing": return this.routing(t);
            case "network": return this.network(t);
            case "interop": return this.interop(t);
            case "errors": return this.errors(t);
            case "diagnostics": return this.diagnostics(t);
            case "runtime": return this.runtime(t);
            default: return this.overview(t);
        }
    }

    private empty(message: string, hint: string | null) {
        return h("div", { class: "bc-empty" }, h("p", null, message), hint ? h("p", { class: "bc-muted" }, hint) : null);
    }

    // ----- actions -----

    private async command(name: string, argument?: string) {
        const t = this.active;
        if (!t) return;
        const answer = await this.source.request(t.info.id, "command", { name, argument });
        this.notice = answer && (answer.error || answer.done) || null;
        await this.poll(true);
    }

    /** Starts picking in the page and waits for its outcome; the hub keeps it, since the extension cannot be called back. */
    private async togglePick() {
        const page = this.source.page;
        if (!page) return;
        if (this.pickTimer) {
            this.endPick();
            await page.stopPick();
            return;
        }
        const since = Date.now();
        if (!(await page.startPick(this.view("components").flag))) {
            const last = await page.lastPick();
            this.notice = last && last.error || "Picking is not available on this page.";
            this.renderStatus();
            return;
        }
        this.notice = "Click a component in the page · Esc cancels";
        this.renderStatus();
        let polls = 0;
        this.pickTimer = setInterval(async () => {
            try {
                const last = await page.lastPick();
                if (last && last.at >= since) {
                    this.endPick();
                    await this.picked(last);
                } else if (++polls > 4 && !(await page.isPicking())) {
                    // Cancelled in the page.
                    this.endPick();
                    this.notice = null;
                    this.renderStatus();
                }
            } catch (err: any) {
                this.endPick();
                this.notice = String(err && err.message || err);
                this.renderStatus();
            }
        }, 250);
        this.renderToolbar();
    }

    private endPick(stopInPage = false) {
        if (!this.pickTimer) return;
        clearInterval(this.pickTimer);
        this.pickTimer = null;
        // Left running, the page would swallow its next click for a frontend that is gone.
        if (stopInPage) this.source.page?.stopPick().catch(() => { });
        this.renderToolbar();
    }

    /**
     * Follows the browser's Elements panel: selecting a node there selects the component that rendered it here. Quiet
     * when the node is not a Blazor component's (most of a page is static markup).
     */
    async revealInspectedElement() {
        const page = this.source.live ? this.source.page : null;
        if (!page || !page.inspectedElement) return;
        try {
            const result = await page.inspectedElement(this.view("components").flag);
            if (result && !result.error && result.instanceId !== undefined) await this.picked(result);
        } catch { /* the page is gone, or navigating */ }
    }

    /** Selects what was picked in the page, switching to the runtime that rendered it. */
    private async picked(last: { targetId?: string; instanceId?: number; error?: string }) {
        if (last.error || last.instanceId === undefined) {
            this.notice = last.error || "Nothing was picked.";
            this.renderStatus();
            return;
        }
        this.notice = null;
        const target = this.targets.find(t => t.info.id === last.targetId) || this.active;
        if (!target) return;
        if (target !== this.active) await this.select(target);
        this.tab = "components";
        this.renderHeader();
        this.renderToolbar();
        await this.inspect(target, last.instanceId);
    }

    private async showInPage(t: TargetModel, instanceId: number, scroll: boolean) {
        const page = this.source.page;
        if (!page) return;
        try {
            const result = await page.highlight(t.info.id, instanceId, scroll);
            if (scroll) {
                this.notice = !result.ok ? result.error || "The component cannot be shown." : result.visible === false ? "It renders nothing visible right now." : null;
                this.renderStatus();
            }
        } catch { /* the page is gone; the targets refresh says so */ }
    }

    private async export(format: string) {
        const t = this.active;
        if (!t) return;
        if (!this.source.live) {
            download("bit-bevtools-session-" + (t.doc?.session?.id ?? "x") + ".json", JSON.stringify(t.doc, null, 2), "application/json");
            return;
        }
        const answer = await this.source.request(t.info.id, "export", { format });
        if (answer && answer.content !== undefined) download(answer.fileName, answer.content, answer.contentType);
        else this.notice = answer && answer.error || "Export failed";
        this.renderStatus();
    }

    private async openFile(file: File) {
        try {
            const doc = JSON.parse(await file.text());
            if (doc && Array.isArray(doc.traceEvents)) {
                this.notice = file.name + " is a performance trace: open it in ui.perfetto.dev or the Chrome DevTools Performance panel.";
                this.renderStatus();
                return;
            }
            if (!doc || typeof doc.format !== "string" || !doc.format.startsWith("bit-bevtools/")) {
                throw new Error("not a BevTools session export (bit-bevtools/1)");
            }
            this.use(new FileSource(doc, file.name));
        } catch (err: any) {
            this.notice = "Could not open " + file.name + ": " + String(err && err.message || err);
            this.renderStatus();
        }
    }

    private async inspect(t: TargetModel, instanceId: number) {
        this.view("components").selected = instanceId;
        if (!this.source.live) {
            t.inspected = null;
            t.inspectedId = instanceId;
            this.renderContent();
            return;
        }
        if (t.inspectedId !== instanceId) t.expanded.clear();
        t.inspectedId = instanceId;
        const answer = await this.source.request(t.info.id, "inspect", { instanceId });
        t.inspected = answer && !answer.error ? answer : null;
        this.renderContent();
        // The runtime asked its page for the symbols (WebAssembly): read again once they can have arrived.
        if (t.inspected && t.inspected.sourcesPending) {
            setTimeout(() => { if (t.inspectedId === instanceId) this.inspect(t, instanceId); }, 1500);
        }
    }

    private async expand(t: TargetModel, section: string, name: string, path: string) {
        const key = section + "/" + name + "/" + path;
        if (t.expanded.has(key)) {
            t.expanded.delete(key);
        } else {
            const answer = await this.source.request(t.info.id, "expand", { instanceId: t.inspectedId, section, name, path });
            t.expanded.set(key, answer && answer.children || []);
        }
        this.renderContent();
    }

    // ----- views -----

    private overview(t: TargetModel) {
        const d = t.doc;
        const s = d.session || {};
        const comps = d.components?.items || [];
        const findings = (d.diagnostics || []).filter((f: any) => !f.dismissed);
        const hottest = comps.filter((c: any) => !c.hidden && c.renders > 0).sort((a: any, b: any) => b.totalRenderMs - a.totalRenderMs).slice(0, 8);
        const http = d.http || [];
        return h("div", { class: "bc-scroll" },
            h("div", { class: "bc-stats" },
                stat(String(d.components?.live ?? comps.length), "live components"),
                stat(String(comps.reduce((n: number, c: any) => n + (c.renders || 0), 0)), "renders recorded"),
                stat(String((d.errors || []).reduce((n: number, e: any) => n + (e.count || 1), 0)), "error occurrences"),
                stat(http.length + " / " + http.filter((r: any) => r.failed).length, "HTTP requests / failed"),
                stat(String(findings.length), "active findings"),
                stat(fmt.ms(d.overhead?.totalMs), "BevTools CPU time")),
            section("Runtimes on this page", h("table", null,
                h("thead", null, h("tr", null, h("th", null, "Runtime"), h("th", null, "Render mode"), h("th", null, "Session"))),
                h("tbody", null, this.targets.map(x => h("tr", { class: x === t ? "bc-selected" : "", onclick: () => this.select(x) },
                    h("td", null, x.info.label), h("td", null, x.info.renderMode || "–"), h("td", null, "#" + (x.info.sessionId ?? "?"))))))),
            section("Session", kv([
                ["Platform", (s.platform || "?") + (s.interactive ? ", interactive" : "") + (s.renderMode ? " · " + s.renderMode : "")],
                ["Started", fmt.time(s.startedAt)],
                ["Value access", s.valueAccess],
                ["Runtime", s.runtime],
                ["Components", s.componentsVersion],
                ["BevTools", s.bevToolsVersion],
                ["Enabled because", s.enabledReason],
            ])),
            findings.length ? section("Findings", findings.slice(0, 6).map((f: any) => this.finding(t, f))) : null,
            hottest.length ? section("Hottest components", this.componentTable(t, hottest)) : null);
    }

    private componentTable(t: TargetModel, rows: any[]) {
        return h("table", null,
            h("thead", null, h("tr", null, h("th", null, "Component"), h("th", { class: "bc-num" }, "Renders"), h("th", { class: "bc-num" }, "Total"), h("th", { class: "bc-num" }, "Max"), h("th", null, "Last cause"))),
            h("tbody", null, rows.map(c => h("tr", { onclick: () => { this.showTab("components"); this.inspect(t, c.instanceId); } },
                h("td", { class: "bc-mono" }, c.displayName, c.disposed ? " †" : ""),
                h("td", { class: "bc-num" }, c.renders),
                h("td", { class: "bc-num" }, fmt.ms(c.totalRenderMs)),
                h("td", { class: "bc-num" }, fmt.ms(c.maxRenderMs)),
                h("td", { class: "bc-muted bc-truncate" }, c.lastCauseDetail || "")))));
    }

    private components(t: TargetModel) {
        const v = this.view();
        const items: any[] = (t.doc.components?.items || []).filter((c: any) => !c.disposed && (v.flag || !c.hidden));
        const byId = new Map<number, any>(items.map(c => [c.instanceId, c]));
        const all = new Map<number, any>((t.doc.components?.items || []).map((x: any) => [x.instanceId, x]));
        const children = new Map<number | null, any[]>();
        for (const c of items) {
            // A hidden parent is skipped: its visible children hang from the nearest visible ancestor.
            let parent = c.parentInstanceId ?? null;
            while (parent !== null && !byId.has(parent)) parent = all.get(parent)?.parentInstanceId ?? null;
            if (!children.has(parent)) children.set(parent, []);
            children.get(parent)!.push(c);
        }
        const rows: Node[] = [];
        const walk = (parent: number | null, depth: number): boolean => {
            let any = false;
            for (const c of children.get(parent) || []) {
                const index = rows.length;
                rows.push(h("div", {
                    class: "bc-tree-row" + (v.selected === c.instanceId ? " bc-selected" : ""),
                    style: "padding-left:" + (depth * 14 + 6) + "px",
                    title: c.type,
                    onclick: () => this.inspect(t, c.instanceId),
                    onmouseenter: () => this.showInPage(t, c.instanceId, false),
                }, h("span", { class: "bc-name" }, c.displayName),
                    c.renderMode ? h("span", { class: "bc-chip bc-info", title: c.renderMode }, "⚡ " + c.renderMode.replace("InteractiveWebAssembly", "WASM").replace("Interactive", "")) : null,
                    c.errors ? h("span", { class: "bc-err" }, " " + c.errors + " ✕") : null,
                    c.renders ? h("span", { class: "bc-meta" }, c.renders + " × · " + fmt.ms(c.lastRenderMs)) : null));
                const childMatched = walk(c.instanceId, depth + 1);
                const self = !v.filter || matches(v.filter, c.displayName, c.type);
                if (!self && !childMatched) rows.splice(index, rows.length - index);
                else any = true;
            }
            return any;
        };
        walk(null, 0);
        const page = this.source.live ? this.source.page : null;
        return h("div", { class: "bc-split" },
            h("div", { class: "bc-scroll bc-left", onmouseleave: page ? () => page.clearHighlight().catch(() => { }) : null }, rows.length ? rows : this.empty("No components match.", null)),
            h("div", { class: "bc-scroll bc-right" }, this.inspector(t, byId.get(v.selected) || (t.doc.components?.items || []).find((c: any) => c.instanceId === v.selected))));
    }

    private inspector(t: TargetModel, c: any) {
        if (!c) return this.empty("Select a component.", "Parameters, cascading values, state, bindings and renders appear here.");
        const d = this.source.live && t.inspected && t.inspected.instanceId === c.instanceId ? t.inspected : null;
        const sources = d && d.sources && d.sources.length
            ? d.sources.map((s: any) => [sourceLink(s), " "])
            : (d && d.sourcesPending ? "loading symbols…" : null);
        const head = h("div", { class: "bc-inspector-head" }, h("strong", { class: "bc-mono" }, c.displayName), h("span", { class: "bc-chip" }, "#" + c.instanceId), c.renderMode ? h("span", { class: "bc-chip bc-info" }, c.renderMode) : null,
            this.source.live ? h("button", { onclick: () => this.inspect(t, c.instanceId) }, "↻") : null,
            this.source.live && this.source.page && !c.disposed ? h("button", { title: "Scroll the page to this component and outline it", onclick: () => this.showInPage(t, c.instanceId, true) }, "⌖ Show in page") : null);
        const stats = kv([
            ["Type", c.type],
            ["Source", sources],
            ["Renders", c.renders + " · total " + fmt.ms(c.totalRenderMs) + " · max " + fmt.ms(c.maxRenderMs)],
            ["Forced re-renders", c.rendersWithComparedParameters ? c.parentRendersWithUnchangedParameters + " of " + c.rendersWithComparedParameters + " compared" : "not compared"],
            ["Last cause", c.lastCauseDetail || "–"],
            ["Created", fmt.time(c.createdAt)],
            ["Instrumented", c.instrumented ? "yes" : (c.instrumentationNote || "no")],
        ]);
        if (!d) {
            return h("div", null, head, stats, h("p", { class: "bc-muted" }, this.source.live ? "Reading the component…" : "An exported session carries render statistics, not values: open the live page to inspect parameters and state."));
        }
        const tree = (sectionName: string, nodes: any[]) => nodes.length
            ? nodes.map(n => this.valueNode(t, sectionName, n.name, n, 0))
            : h("div", { class: "bc-muted" }, "none");
        return h("div", null, head, stats,
            section("Parameters (" + d.parameters.length + ")", tree("parameters", d.parameters)),
            section("Cascading values (" + d.cascading.length + ")", tree("cascading", d.cascading)),
            section("State (" + d.state.length + " fields)", tree("state", d.state)),
            section("Event handlers & callbacks (" + d.wiring.total + ")", d.wiring.unavailable
                ? h("div", { class: "bc-muted" }, d.wiring.unavailable)
                : h("table", null, h("tbody", null, d.wiring.bindings.map((b: any) => h("tr", null,
                    h("td", { class: "bc-mono" }, b.childInstanceId ? h("a", { href: "#", onclick: (e: Event) => { e.preventDefault(); this.inspect(t, b.childInstanceId); } }, b.target) : b.target),
                    h("td", { class: "bc-mono" }, b.event, b.kind === "ChildCallback" ? h("span", { class: "bc-chip" }, "parameter") : null),
                    h("td", { class: "bc-mono" }, b.handler, b.handlerSource && b.handlerSource[0] ? [" ", sourceLink(b.handlerSource[0])] : null)))))),
            section("Why did it render?", kv(Object.keys(d.causes).map(k => [k, String(d.causes[k])] as [string, string]))),
            section("Recent renders", h("table", null, h("tbody", null, d.recentRenders.map((r: any) => h("tr", { class: r.failed ? "bc-err" : "" },
                h("td", { class: "bc-mono" }, fmt.time(r.at)),
                h("td", { class: "bc-num" }, fmt.ms(r.durationMs)),
                h("td", null, r.forced ? h("span", { class: "bc-chip bc-warn" }, "forced") : null, r.detail),
                h("td", { class: "bc-muted bc-mono" }, (r.changedParameters || []).concat(r.changedState || []).join("; "))))))),
            section("Injected services (" + d.services.length + ")", h("table", null, h("tbody", null, d.services.map((s: any) => h("tr", null,
                h("td", { class: "bc-mono" }, s.name), h("td", { class: "bc-mono" }, s.service), h("td", null, s.lifetime || "?"), h("td", { class: "bc-muted bc-mono" }, s.implementation || ""))))), true));
    }

    private valueNode(t: TargetModel, sectionName: string, rootName: string, node: any, depth: number): Node {
        const key = sectionName + "/" + rootName + "/" + node.path;
        const open = t.expanded.has(key);
        const row = h("div", { class: "bc-vt-row", style: "padding-left:" + depth * 14 + "px", title: node.type || "" },
            node.expandable ? h("span", { class: "bc-caret", onclick: () => this.expand(t, sectionName, rootName, node.path) }, open ? "▾" : "▸") : h("span", { class: "bc-caret" }),
            h("span", { class: "bc-vt-name" }, node.name + ": "),
            h("span", { class: "bc-vt-value bc-k-" + String(node.kind).toLowerCase() }, node.display));
        if (!open) return row;
        return h("div", null, row, (t.expanded.get(key) || []).map(child => this.valueNode(t, sectionName, rootName, child, depth + 1)));
    }

    private profiler(t: TargetModel) {
        const v = this.view();
        let rows: any[] = (t.doc.components?.items || []).filter((c: any) => !c.hidden && c.renders > 0 && matches(v.filter, c.displayName, c.type));
        if (v.flag) {
            const groups = new Map<string, any>();
            for (const c of rows) {
                const g = groups.get(c.type) || { displayName: c.displayName, type: c.type, instances: 0, renders: 0, totalRenderMs: 0, maxRenderMs: 0 };
                g.instances++;
                g.renders += c.renders;
                g.totalRenderMs += c.totalRenderMs;
                g.maxRenderMs = Math.max(g.maxRenderMs, c.maxRenderMs);
                groups.set(c.type, g);
            }
            rows = Array.from(groups.values());
        }
        const sort = v.sort || "total";
        const key: { [k: string]: (c: any) => number } = {
            total: c => c.totalRenderMs, renders: c => c.renders, max: c => c.maxRenderMs, avg: c => c.totalRenderMs / Math.max(1, c.renders),
            forced: c => c.parentRendersWithUnchangedParameters || 0,
        };
        rows.sort((a, b) => key[sort](b) - key[sort](a));
        const header = (id: string, title: string) => h("th", { class: "bc-num bc-sortable" + (sort === id ? " bc-active" : ""), onclick: () => { v.sort = id; this.renderContent(); } }, title);
        return h("div", { class: "bc-scroll" }, h("table", null,
            h("thead", null, h("tr", null, h("th", null, v.flag ? "Component type" : "Component"), v.flag ? h("th", { class: "bc-num" }, "Instances") : null,
                header("renders", "Renders"), header("total", "Total"), header("avg", "Avg"), header("max", "Max"), v.flag ? null : header("forced", "Forced"), v.flag ? null : h("th", null, "Last cause"))),
            h("tbody", null, rows.slice(0, MAX_ROWS).map(c => h("tr", { onclick: v.flag ? undefined : () => { this.showTab("components"); this.inspect(t, c.instanceId); } },
                h("td", { class: "bc-mono", title: c.type }, c.displayName),
                v.flag ? h("td", { class: "bc-num" }, c.instances) : null,
                h("td", { class: "bc-num" }, c.renders),
                h("td", { class: "bc-num" }, fmt.ms(c.totalRenderMs)),
                h("td", { class: "bc-num" }, fmt.ms(c.totalRenderMs / Math.max(1, c.renders))),
                h("td", { class: "bc-num" }, fmt.ms(c.maxRenderMs)),
                v.flag ? null : h("td", { class: "bc-num" }, c.rendersWithComparedParameters ? c.parentRendersWithUnchangedParameters + "/" + c.rendersWithComparedParameters : "–"),
                v.flag ? null : h("td", { class: "bc-muted bc-truncate" }, c.lastCauseDetail || ""))))));
    }

    private timeline(t: TargetModel) {
        const v = this.view();
        const events: any[] = t.events;
        const byId = new Map<number, any>(events.map(e => [e.id, e]));
        const shown = events.filter(e => matches(v.filter, e.title, e.kind, e.category, e.componentName, e.detail)).slice(-MAX_ROWS).reverse();
        const selected = byId.get(v.selected);
        const list = h("div", { class: "bc-scroll bc-left" }, shown.length ? h("table", null, h("tbody", null, shown.map(e => h("tr", {
            class: "bc-sev-" + String(e.severity).toLowerCase() + (v.selected === e.id ? " bc-selected" : ""),
            onclick: () => { v.selected = e.id; this.renderContent(); },
        }, h("td", { class: "bc-mono bc-muted" }, fmt.time(e.timestamp)),
            h("td", null, h("span", { class: "bc-kind bc-kind-" + String(e.kind).toLowerCase() }, e.kind)),
            h("td", { class: "bc-truncate" }, e.title),
            h("td", { class: "bc-num" }, e.durationMs !== undefined ? fmt.ms(e.durationMs) : ""))))) : this.empty("No events match.", null));
        let detail: Node = this.empty("Select an event.", "What caused it and what it caused appear here.");
        if (selected) {
            const causedBy: any[] = [];
            for (let p = byId.get(selected.parentEventId); p && causedBy.length < 8; p = byId.get(p.parentEventId)) causedBy.push(p);
            const caused = events.filter(e => e.parentEventId === selected.id).slice(0, 50);
            const link = (e: any) => h("div", { class: "bc-link", onclick: () => { v.selected = e.id; this.renderContent(); } }, fmt.time(e.timestamp) + " · " + e.title);
            detail = h("div", null,
                h("h3", null, selected.title),
                kv([["Kind", selected.kind + " · " + selected.category], ["Time", fmt.time(selected.timestamp)], ["Duration", fmt.ms(selected.durationMs)], ["Severity", selected.severity], ["Component", selected.componentName || "–"], ["Detail", selected.detail || "–"]]),
                selected.data ? section("Data", kv(Object.keys(selected.data).map(k => [k, typeof selected.data[k] === "object" ? JSON.stringify(selected.data[k]) : String(selected.data[k])] as [string, string]))) : null,
                causedBy.length ? section("Caused by", causedBy.map(link)) : null,
                caused.length ? section("Caused (" + caused.length + ")", caused.map(link)) : null);
        }
        return h("div", { class: "bc-split" }, list, h("div", { class: "bc-scroll bc-right" }, detail));
    }

    private forms(t: TargetModel) {
        const forms: any[] = t.doc.forms || [];
        if (!forms.length) return this.empty("No form on the page.", "Forms appear as their EditForm renders: fields, validations, what validates them and what shows their messages. Values are never read.");
        return h("div", { class: "bc-scroll" }, forms.map(f => section(
            (f.owner || "?") + " · " + f.model + (f.lastValidationPassed === undefined ? " · not validated yet" : f.lastValidationPassed ? " · valid" : " · " + f.messages + " messages"),
            h("div", null,
                kv([
                    ["Validated by", f.validators.length ? f.validators.join(", ") : (f.validationAttributes.length ? "nothing - its validation attributes are never checked" : "nothing")],
                    ["Messages shown by", f.messageDisplays.length ? f.messageDisplays.join(", ") : "no ValidationSummary or ValidationMessage"],
                    ["Rules on the model", f.validationAttributes.join("; ")],
                    ["Validations", String(f.validations)],
                ]),
                f.fields.length ? h("table", null,
                    h("thead", null, h("tr", null, h("th", null, "Field"), h("th", null, "Modified"), h("th", { class: "bc-num" }, "Changes"), h("th", null, "Messages"))),
                    h("tbody", null, f.fields.map((x: any) => h("tr", null,
                        h("td", { class: "bc-mono" }, x.name), h("td", null, x.modified ? "yes" : ""), h("td", { class: "bc-num" }, x.changes || ""), h("td", null, (x.messages || []).join(" · ")))))) : null))));
    }

    private state(t: TargetModel) {
        const v = this.view();
        const providers: any[] = (t.doc.state || []).filter((p: any) => matches(v.filter, p.provider, p.description));
        if (!providers.length) return this.empty("No state providers.", "Implement IBevStateProvider, or register one with AddBevToolsStateProvider; the authentication state appears here on its own.");
        return h("div", { class: "bc-scroll" }, providers.map(p => section(p.provider + " · " + p.changeCount + " changes", [
            p.error ? h("div", { class: "bc-err" }, p.error) : null,
            h("div", { class: "bc-muted" }, (p.description || "") + " · " + p.origin),
            (p.changes || []).slice().reverse().slice(0, 50).map((c: any) => h("div", { class: "bc-change" },
                h("div", null, h("span", { class: "bc-mono bc-muted" }, fmt.time(c.at) + " "), h("strong", null, c.action || "changed"), c.detail ? " · " + c.detail : ""),
                h("ul", { class: "bc-mono" }, (c.diff || []).map((d: any) => h("li", null, d.path + ": " + (d.before ?? "∅") + " → " + (d.after ?? "∅")))))),
        ])));
    }

    private routing(t: TargetModel) {
        const v = this.view();
        const r = t.doc.routing;
        if (!r || typeof r !== "object") return this.empty("No routing information in this session.", null);
        const c = r.current || {};
        const routes: any[] = (r.routes || []).filter((x: any) => matches(v.filter, (x.templates || []).join(" "), x.page, (x.authorization || []).join(" ")));
        const pairs = (o: any) => Object.keys(o || {}).map(k => [k, String(o[k])] as [string, string]);
        return h("div", { class: "bc-scroll" },
            section("Current route", [kv([["URI", c.uri || "unknown"], ["Page", c.page || "–"], ["Template", (c.template || "unknown") + (c.templateInferred ? " (inferred: the page's only @page)" : "")], ["Layout", c.layout || "–"], ["Source", c.source]]),
                Object.keys(c.routeValues || {}).length ? section("Route values", kv(pairs(c.routeValues))) : null,
                Object.keys(c.query || {}).length ? section("Query", kv(pairs(c.query))) : null]),
            section("Route table · " + routes.length, [h("div", { class: "bc-muted" }, r.routesSource), h("table", null, h("tbody", null, routes.map(x => h("tr", { class: x.page === c.page ? "bc-selected" : "" },
                h("td", { class: "bc-mono" }, (x.templates || []).join(" · ")),
                h("td", { class: "bc-mono" }, String(x.page).split(".").pop()),
                h("td", null, (x.authorization || []).map((a: string) => h("span", { class: "bc-chip bc-warn" }, a)), x.allowAnonymous ? h("span", { class: "bc-chip" }, "AllowAnonymous") : null),
                h("td", null, x.renderMode ? h("span", { class: "bc-chip" }, x.renderMode) : null)))))]));
    }

    private network(t: TargetModel) {
        const v = this.view();
        const requests: any[] = (t.doc.http || []).filter((r: any) => matches(v.filter, r.url, r.method, r.error, r.trigger, r.caller)).slice().reverse();
        const selected = requests.find(r => r.id === v.selected);
        const sockets = this.sockets;
        // Per endpoint, ids in the path as {id}: the runtime groups them (BevHttpEndpoints), the busiest first.
        const endpoints: any[] = (t.doc.httpEndpoints || []).filter((e: any) => matches(v.filter, e.endpoint));
        return h("div", { class: "bc-split" },
            h("div", { class: "bc-scroll bc-left" },
                endpoints.length ? section("Endpoints · " + endpoints.length, h("table", null,
                    h("thead", null, h("tr", null, h("th", null, "Endpoint"), h("th", { class: "bc-num" }, "Requests"), h("th", { class: "bc-num" }, "Failed"), h("th", { class: "bc-num" }, "p50"), h("th", { class: "bc-num" }, "p95"), h("th", null, "Sent from"))),
                    h("tbody", null, endpoints.slice(0, 50).map(e => h("tr", { class: e.failed ? "bc-sev-error" : "" },
                        h("td", { class: "bc-mono bc-truncate", title: e.endpoint }, fmt.endpoint(e.endpoint)),
                        h("td", { class: "bc-num" }, e.requests),
                        h("td", { class: "bc-num" }, e.failed + (e.cancelled ? " · " + e.cancelled + " cancelled" : "")),
                        h("td", { class: "bc-num" }, e.p50Ms === undefined ? "–" : fmt.ms(e.p50Ms)),
                        h("td", { class: "bc-num" }, e.p95Ms === undefined ? "–" : fmt.ms(e.p95Ms)),
                        h("td", { class: "bc-mono bc-truncate", title: (e.callers || []).map((c: any) => c.component + " ×" + c.requests).join(", ") },
                            (e.callers || []).length ? e.callers[0].component + (e.callers[0].requests > 1 ? " ×" + e.callers[0].requests : "") + ((e.callerCount ?? e.callers.length) > 1 ? " +" + ((e.callerCount ?? e.callers.length) - 1) : "") : "–")))))) : null,
                requests.length ? h("table", null,
                    h("thead", null, h("tr", null, h("th", null, "Time"), h("th", null, "Method"), h("th", null, "URL"), h("th", { class: "bc-num" }, "Status"), h("th", { class: "bc-num" }, "Duration"), h("th", { class: "bc-num" }, "Size"))),
                    h("tbody", null, requests.slice(0, MAX_ROWS).map(r => h("tr", { class: (r.failed ? "bc-sev-error" : "") + (v.selected === r.id ? " bc-selected" : ""), onclick: () => { v.selected = r.id; this.renderContent(); } },
                        h("td", { class: "bc-mono" }, fmt.time(r.startedAt)), h("td", { class: "bc-mono" }, r.method), h("td", { class: "bc-mono bc-truncate", title: r.url }, fmt.path(r.url)),
                        h("td", { class: "bc-num" }, r.error ? "ERR" : r.statusCode ?? "…"), h("td", { class: "bc-num" }, r.pending ? "pending" : fmt.ms(r.durationMs)), h("td", { class: "bc-num" }, fmt.bytes(r.responseBytes))))))
                    : this.empty("No HTTP requests.", null),
                section("WebSockets · " + sockets.length, sockets.length ? sockets.map(s => this.socket(s)) : h("div", { class: "bc-muted" }, "No WebSocket observed yet."))),
            h("div", { class: "bc-scroll bc-right" }, selected ? h("div", null,
                h("h3", { class: "bc-mono" }, selected.method + " " + fmt.path(selected.url)),
                kv([["URL", selected.url], ["Status", selected.error || (selected.statusCode + " " + (selected.reasonPhrase || ""))], ["Duration", fmt.ms(selected.durationMs)], ["Trigger", selected.trigger || "–"], ["Sent from", selected.caller || selected.callerMissing || this.noCaller(t)], ["Content type", selected.contentType || "–"]]),
                (selected.requestHeaders || []).length ? section("Request headers", kv(selected.requestHeaders.map((x: any) => [x.name, x.value]))) : null,
                (selected.responseHeaders || []).length ? section("Response headers", kv(selected.responseHeaders.map((x: any) => [x.name, x.value]))) : null)
                : this.empty("Select a request.", "Bodies are never captured; sensitive headers and query values are redacted.")));
    }

    // A request with neither a caller nor the reason there is none: capture off, or an export from before either was recorded.
    private noCaller(t: TargetModel) {
        return t.doc.options?.captureHttpCallers === false ? "not captured (CaptureHttpCallers is off)" : "–";
    }

    private socket(s: any) {
        const rb = s.renderBatches || {};
        const messages = Object.keys(s.messages || {}).map(k => [k, s.messages[k]] as [string, any]).sort((a, b) => b[1].bytes - a[1].bytes);
        return h("div", { class: "bc-socket" },
            h("div", null, h("span", { class: "bc-chip bc-info" }, s.kind), " ", h("span", { class: "bc-mono" }, s.url), s.closedAt ? h("span", { class: "bc-chip bc-warn" }, " closed " + s.closeCode) : h("span", { class: "bc-chip" }, "open")),
            kv([
                ["Protocol", s.protocol || "?"],
                ["Sent", s.sent.count + " · " + fmt.bytes(s.sent.bytes)],
                ["Received", s.received.count + " · " + fmt.bytes(s.received.bytes)],
                ["BevTools' own", s.own.count + " · " + fmt.bytes(s.own.bytes)],
                rb.count ? ["Render batches", rb.count + " · " + fmt.bytes(rb.bytes) + " · largest " + fmt.bytes(rb.maxBytes) + " · applied in " + fmt.ms(rb.applyMs / Math.max(1, rb.count)) + " avg, " + fmt.ms(rb.maxApplyMs) + " max"] : null,
            ].filter(Boolean) as [string, string][]),
            h("table", null, h("tbody", null, messages.slice(0, 20).map(([k, m]) => h("tr", null, h("td", { class: "bc-mono" }, k), h("td", { class: "bc-num" }, m.count), h("td", { class: "bc-num" }, fmt.bytes(m.bytes)), h("td", { class: "bc-num" }, fmt.bytes(m.maxBytes)))))));
    }

    private interop(t: TargetModel) {
        const v = this.view();
        const aggregates: any[] = (t.doc.jsInterop?.aggregates || []).filter((a: any) => matches(v.filter, a.identifier));
        const calls: any[] = (t.doc.jsInterop?.calls || []).filter((c: any) => matches(v.filter, c.identifier, c.componentName)).slice(-200).reverse();
        return h("div", { class: "bc-scroll" },
            section("By identifier", h("table", null,
                h("thead", null, h("tr", null, h("th", null, "Identifier"), h("th", null, "Direction"), h("th", { class: "bc-num" }, "Calls"), h("th", { class: "bc-num" }, "Total"), h("th", { class: "bc-num" }, "Max"), h("th", { class: "bc-num" }, "Failures"))),
                h("tbody", null, aggregates.map(a => h("tr", null, h("td", { class: "bc-mono" }, a.identifier), h("td", null, a.direction === "JsToDotNet" ? "JS → .NET" : ".NET → JS"),
                    h("td", { class: "bc-num" }, a.count), h("td", { class: "bc-num" }, fmt.ms(a.totalMs)), h("td", { class: "bc-num" }, fmt.ms(a.maxMs)), h("td", { class: "bc-num" + (a.failures ? " bc-err" : "") }, a.failures)))))),
            section("Recent calls", h("table", null, h("tbody", null, calls.map(c => h("tr", { class: c.succeeded ? "" : "bc-sev-error" },
                h("td", { class: "bc-mono" }, fmt.time(c.at)), h("td", { class: "bc-mono" }, c.identifier), h("td", { class: "bc-num" }, fmt.ms(c.durationMs)), h("td", { class: "bc-muted" }, c.componentName || ""), h("td", { class: "bc-err" }, c.error || "")))))));
    }

    private errors(t: TargetModel) {
        const v = this.view();
        const errors: any[] = (t.doc.errors || []).filter((e: any) => matches(v.filter, e.message, e.exceptionType, e.source, e.componentName)).slice().reverse();
        const selected = errors.find(e => e.id === v.selected);
        // One problem with different values in its messages: only the groups that merged something are worth a row.
        const groups: any[] = (t.doc.errorGroups || []).filter((g: any) => g.distinctMessages > 1 && matches(v.filter, g.messageTemplate, g.exceptionType, g.source));
        return h("div", { class: "bc-split" },
            h("div", { class: "bc-scroll bc-left" },
                groups.length ? section("Groups · one problem, different values", h("table", null,
                    h("thead", null, h("tr", null, h("th", null, "Error"), h("th", { class: "bc-num" }, "Messages"), h("th", { class: "bc-num" }, "×"), h("th", null, "Where"))),
                    h("tbody", null, groups.slice(0, 50).map(g => h("tr", { class: "bc-sev-error" },
                        h("td", { class: "bc-truncate", title: g.messageTemplate }, String(g.exceptionType).split(".").pop() + ": " + g.messageTemplate),
                        h("td", { class: "bc-num" }, g.distinctMessages),
                        h("td", { class: "bc-num" }, g.occurrences),
                        h("td", { class: "bc-mono bc-muted" }, g.sourceLocation || "–")))))) : null,
                errors.length ? h("table", null, h("tbody", null, errors.map(e => h("tr", { class: v.selected === e.id ? "bc-selected" : "", onclick: () => { v.selected = e.id; this.renderContent(); } },
                h("td", { class: "bc-mono" }, fmt.time(e.lastSeen)), h("td", null, h("span", { class: "bc-chip" }, e.source)), h("td", { class: "bc-truncate" }, String(e.exceptionType).split(".").pop() + ": " + e.message), h("td", { class: "bc-num" }, e.count > 1 ? "×" + e.count : ""))))) : this.empty("No errors recorded.", null)),
            h("div", { class: "bc-scroll bc-right" }, selected ? h("div", null,
                h("h3", null, String(selected.exceptionType).split(".").pop() + ": " + selected.message),
                kv([["Source", selected.source], ["Count", String(selected.count)], ["First seen", fmt.time(selected.firstSeen)], ["Component", selected.componentName || "–"], ["Location", selected.sourceLocation || "–"], ["Inner", selected.innerError || "–"]]),
                selected.stackTrace ? h("pre", { class: "bc-pre" }, selected.stackTrace) : null) : this.empty("Select an error.", null)));
    }

    private diagnostics(t: TargetModel) {
        const findings: any[] = (t.doc.diagnostics || []).filter((f: any) => !f.dismissed);
        if (!findings.length) return this.empty("No active findings.", "Rules run as activity arrives: render storms, forced re-renders, slow handlers, duplicate requests, data fetched twice while prerendering, large render batches…");
        return h("div", { class: "bc-scroll" }, findings.map(f => this.finding(t, f)));
    }

    private finding(t: TargetModel, f: any) {
        return h("div", { class: "bc-finding bc-sev-" + String(f.severity).toLowerCase() },
            h("div", { class: "bc-finding-title" }, h("span", { class: "bc-chip" }, f.severity), " ", f.title, h("span", { class: "bc-muted bc-mono" }, " " + f.ruleId)),
            h("div", null, f.evidence),
            f.suggestion ? h("div", { class: "bc-suggestion" }, "→ " + f.suggestion) : null,
            this.source.live ? h("button", { onclick: () => this.command("dismiss", f.fingerprint) }, "Dismiss") : null,
            f.componentInstanceId ? h("button", { onclick: () => { this.showTab("components"); this.inspect(t, f.componentInstanceId); } }, "Component ↗") : null);
    }

    private runtime(t: TargetModel) {
        const sections: any[] = t.doc.runtime || [];
        return h("div", { class: "bc-scroll" }, sections.map(s => section(s.title + (s.scope ? " · " + s.scope : ""),
            h("dl", { class: "bc-kv" }, s.facts.map((f: any) => [h("dt", null, f.name), h("dd", { class: "bc-sev-" + String(f.severity).toLowerCase() }, f.value, f.hint ? h("div", { class: "bc-muted" }, f.hint) : null)])))),
            t.doc.capabilities ? section("Capabilities", h("table", null, h("tbody", null, t.doc.capabilities.map((c: any) => h("tr", null,
                h("td", null, c.area), h("td", null, c.name), h("td", { class: "bc-cap-" + String(c.status).toLowerCase() }, c.status), h("td", { class: "bc-muted" }, c.explanation))))), true) : null);
    }
}

/** "Counter.razor:12": a link into the editor the app configured, never one that runs script. */
function sourceLink(source: any): Node {
    const text = source.file + ":" + source.line;
    const url = typeof source.url === "string" && /^[a-z][a-z0-9+.-]*:/i.test(source.url) && !/^(javascript|data|vbscript):/i.test(source.url.trim()) ? source.url : null;
    const moved = source.mayHaveMoved ? " (as built: edited with hot reload since, so the line may have moved)" : "";
    const link = url ? h("a", { class: "bc-mono", href: url, title: "Open " + source.path + " in your editor" + moved }, text) : h("span", { class: "bc-mono", title: source.path + moved }, text);
    return moved ? h("span", null, link, h("span", { class: "bc-muted" }, " (before hot reload)")) : link;
}

function stat(value: string, label: string) {
    return h("div", { class: "bc-stat" }, h("div", { class: "bc-stat-value" }, value), h("div", { class: "bc-stat-label" }, label));
}

function section(title: string, body: Child, collapsed = false) {
    const details = h("details", { class: "bc-section", open: !collapsed }, h("summary", null, title), h("div", { class: "bc-section-body" }, body));
    return details;
}

function kv(pairs: ([string, Child] | null)[]) {
    return h("dl", { class: "bc-kv" }, pairs.filter(Boolean).map(p => [h("dt", null, p![0]), h("dd", null, p![1] === null || p![1] === undefined || p![1] === "" ? "–" : p![1])]));
}

// companion.html marks itself, so the module starts there without an inline script; the browser extension's panel
// imports the module and starts a Companion with its own source instead.
if (typeof document !== "undefined" && document.documentElement && document.documentElement.hasAttribute("data-bev-companion")) {
    boot();
}

/** Starts the companion in this page: on the opener's hub when one opened it, otherwise ready to open an exported file. */
export function boot(root?: HTMLElement) {
    let source: CompanionSource;
    try {
        source = window.opener && !window.opener.closed && window.opener.__BIT_BEVTOOLS__ ? new OpenerSource() : new EmptySource();
    } catch {
        // A cross-origin opener: nothing of it is readable, which is the browser keeping the page safe.
        source = new EmptySource();
    }
    const companion = new Companion(source, root);
    companion.start();
    window.addEventListener("pagehide", () => companion.stop());
    return companion;
}
