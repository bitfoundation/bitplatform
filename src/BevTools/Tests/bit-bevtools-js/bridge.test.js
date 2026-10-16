// Tests for the BevTools browser bridge (bit-bevtools.js): browser-side redaction, downloads, JS -> .NET call
// tracking and its isolation from BevTools' own traffic, and the keyboard shortcuts.
//
// They run against wwwroot/bit-bevtools.js, i.e. the built output rather than the TypeScript, so build Bit.BevTools
// first (CI does): that file is what ships in the package. The bridge keeps module-level state (the attached
// reference, the wrapped DotNet functions), so every test imports a fresh copy of the module.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, it, expect, vi } from 'vitest';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const BUNDLE = path.resolve(HERE, '../../Bit.BevTools/wwwroot/bit-bevtools.js');

async function loadBridge() {
    if (!fs.existsSync(BUNDLE)) {
        throw new Error(`Missing ${BUNDLE}.\nBuild the package first: dotnet build ../../Bit.BevTools/Bit.BevTools.csproj`);
    }
    vi.resetModules();
    return import(BUNDLE);
}

class Storage {
    constructor(values = {}) {
        this.values = values;
    }

    get length() {
        return Object.keys(this.values).length;
    }

    key(index) {
        return Object.keys(this.values)[index];
    }

    getItem(key) {
        return this.values[key] ?? null;
    }

    setItem(key, value) {
        this.values[key] = value;
    }
}

function installBrowser({ localStorage = new Storage(), sessionStorage = new Storage() } = {}) {
    const listeners = new Map();
    const eventTarget = {
        addEventListener(type, handler) {
            listeners.set(type, handler);
        },
        removeEventListener(type) {
            listeners.delete(type);
        },
    };

    globalThis.window = {
        ...eventTarget,
        localStorage,
        sessionStorage,
        DotNet: {
            invokeMethodAsync: () => Promise.resolve(null),
            invokeMethod: () => null,
        },
    };
    globalThis.document = { ...eventTarget, visibilityState: 'visible' };
    Object.defineProperty(globalThis, 'navigator', {
        value: { onLine: true, userAgent: 'node' },
        configurable: true,
    });
    return listeners;
}

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

describe('getStorage', () => {
    it('redacts values before they leave the browser', async () => {
        installBrowser({
            localStorage: new Storage({
                profile: '{"access_token":"secret"}',
                session: 'eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature',
                envelope: '{"user":"alice","value":"prefix eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature"}',
                settings: 'password=hunter2',
                cachedRequest: 'Authorization: Bearer secret',
                nested: '{"credential":"opaque-value"}',
                keyMaterial: '-----BEGIN PRIVATE KEY-----\nopaque',
                theme: 'dark',
            }),
        });
        const bridge = await loadBridge();

        const entries = bridge.getStorage('local', []);
        const redacted = key => entries.find(entry => entry.key === key).redacted;

        for (const key of ['profile', 'session', 'envelope', 'settings', 'cachedRequest', 'nested', 'keyMaterial']) {
            expect(redacted(key), key).toBe(true);
        }
        expect(redacted('theme')).toBe(false);
    });
});

describe('downloadText', () => {
    it('hands the export to the browser and cleans up after itself', async () => {
        installBrowser();
        const clicks = [];
        const appended = [];
        let removed = 0;
        const anchor = {
            style: {},
            click() { clicks.push({ href: this.href, download: this.download, rel: this.rel }); },
            remove() { removed++; },
        };
        globalThis.document.createElement = tag => {
            expect(tag).toBe('a');
            return anchor;
        };
        globalThis.document.body = { appendChild: node => appended.push(node) };
        const revoked = [];
        const originalRevoke = URL.revokeObjectURL;
        URL.revokeObjectURL = url => revoked.push(url);
        try {
            const bridge = await loadBridge();

            const ok = bridge.downloadText('session.json', '{"format":"bit-bevtools/1"}', 'application/json');

            expect(ok).toBe(true);
            expect(appended).toHaveLength(1);
            expect(clicks).toHaveLength(1);
            expect(clicks[0].download).toBe('session.json');
            expect(clicks[0].rel).toBe('noopener');
            expect(clicks[0].href).toMatch(/^blob:/);
            await delay(10);
            expect(removed).toBe(1);
            expect(revoked).toEqual([clicks[0].href]);
        } finally {
            URL.revokeObjectURL = originalRevoke;
        }
    });

    it('reports failure instead of throwing when the DOM is unavailable', async () => {
        installBrowser();
        globalThis.document.createElement = () => { throw new Error('no DOM'); };
        const bridge = await loadBridge();

        expect(bridge.downloadText('x.txt', 'content')).toBe(false);
    });
});

describe('JS -> .NET call tracking', () => {
    it('drops pending calls on detach and never traces the bridge callbacks', async () => {
        installBrowser();
        const bridge = await loadBridge();
        let resolveFirst;
        window.DotNet.invokeMethodAsync = (assembly, method) =>
            assembly === 'App' && method === 'First'
                ? new Promise(resolve => { resolveFirst = resolve; })
                : Promise.resolve(null);
        const firstReports = [];
        const firstRef = { invokeMethodAsync: (...args) => { firstReports.push(args); return Promise.resolve(); } };

        bridge.attach(firstRef, { trackJsToDotNet: true });
        const firstCall = window.DotNet.invokeMethodAsync('App', 'First');
        bridge.detach();

        const secondReports = [];
        const secondRef = { invokeMethodAsync: (...args) => { secondReports.push(args); return Promise.resolve(); } };
        bridge.attach(secondRef, { trackJsToDotNet: true });
        resolveFirst();
        await firstCall;
        await window.DotNet.invokeMethodAsync(secondRef, 'OnBrowserEvent', 'online', null);
        await window.DotNet.invokeMethodAsync('App', 'Second');
        await delay(550);

        const batches = secondReports.filter(args => args[0] === 'ReportJsToDotNet');
        expect(batches).toHaveLength(1);
        expect(batches[0][1].map(entry => entry.identifier)).toEqual(['App.Second']);
        bridge.detach();
    });

    it("tracks calls on a DotNetObjectReference, but not the bridge's own reference", async () => {
        installBrowser();
        const dispatched = [];
        class DotNetObject {
            constructor(id) { this._id = id; }
            invokeMethodAsync(method, ...args) { dispatched.push([this._id, method, ...args]); return Promise.resolve(method === 'Fail' ? Promise.reject(new Error('boom')) : null); }
            invokeMethod(method, ...args) { dispatched.push([this._id, method, ...args]); return null; }
        }
        window.DotNet.DotNetObject = DotNetObject;
        const originalAsync = DotNetObject.prototype.invokeMethodAsync;
        const bridge = await loadBridge();
        const bridgeRef = new DotNetObject(1);
        const appRef = new DotNetObject(7);

        bridge.attach(bridgeRef, { trackJsToDotNet: true });
        await appRef.invokeMethodAsync('OnTick', 42);
        appRef.invokeMethod('Sync');
        await expect(appRef.invokeMethodAsync('Fail')).rejects.toThrow('boom');
        await delay(550);

        await new DotNetObject(3).invokeMethodAsync('DispatchEventAsync', {}, {});
        await new DotNetObject(4).invokeMethodAsync('OnSpacerAfterVisible', 0, 0, 0);
        await delay(550);

        const reports = dispatched.filter(call => call[0] === 1 && call[1] === 'ReportJsToDotNet');
        expect(reports, 'framework event dispatch and Virtualize plumbing are not application interop').toHaveLength(1);
        const entries = reports[0][2];
        expect(entries.map(entry => [entry.identifier, entry.args, entry.sync])).toEqual([['object#7.OnTick', 1, false], ['object#7.Sync', 0, true], ['object#7.Fail', 0, false]]);
        expect(entries[2].error).toBe('boom');
        expect(dispatched.some(call => call[0] === 7 && call[1] === 'OnTick' && call[2] === 42), 'the application call still reaches .NET').toBe(true);

        bridge.detach();
        expect(DotNetObject.prototype.invokeMethodAsync).toBe(originalAsync);
    });
});

describe('error reporting', () => {
    it('does not report a send failure on a dead circuit connection back over that connection', async () => {
        const listeners = installBrowser();
        const reports = [];
        const bridge = await loadBridge();
        bridge.attach({ invokeMethodAsync: (method, payload) => { reports.push([method, payload && payload.message]); return Promise.resolve(); } }, {});

        listeners.get('unhandledrejection')({ reason: new Error("Cannot send data if the connection is not in the 'Connected' State.") });
        listeners.get('error')({ error: new Error("Cannot send data if the connection is not in the 'Connected' State.") });
        listeners.get('unhandledrejection')({ reason: new Error('application failure') });

        expect(reports).toEqual([['OnJsError', 'application failure']]);
        bridge.detach();
    });
});

describe('keyboard shortcuts', () => {
    it('lets Ctrl+K open the palette wherever focus is, but only while the panel is open', async () => {
        const listeners = installBrowser();
        let panelOpen = false;
        globalThis.document.querySelector = selector => (selector === '.bev-root .bev-panel' && panelOpen ? {} : null);
        const reports = [];
        const bridge = await loadBridge();
        bridge.attach({ invokeMethodAsync: method => { reports.push(method); return Promise.resolve(); } }, {});
        const press = (modifiers = {}) => {
            const e = { key: 'k', ctrlKey: true, shiftKey: false, altKey: false, metaKey: false, ...modifiers, prevented: false, preventDefault() { this.prevented = true; } };
            listeners.get('keydown')(e);
            return e;
        };

        expect(press().prevented, 'the application keeps Ctrl+K while the panel is closed').toBe(false);
        panelOpen = true;
        expect(press().prevented).toBe(true);
        expect(press({ ctrlKey: false, metaKey: true }).prevented).toBe(true);
        expect(press({ shiftKey: true }).prevented).toBe(false);
        expect(reports).toEqual(['OnPaletteShortcut', 'OnPaletteShortcut']);
        bridge.detach();
    });
});
