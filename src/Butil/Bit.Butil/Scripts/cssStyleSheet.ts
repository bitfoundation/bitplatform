var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Stylesheets this module made, so .NET can go on editing one after it is in the document.
    const _sheets: { [id: string]: { sheet?: any; element?: HTMLStyleElement } } = {};

    // A <style> element's CSSStyleSheet is not stable: setting its text content discards the sheet
    // it had and builds a new one, so an element-backed entry has to be read through the element
    // every time rather than cached at creation.
    function sheetOf(entry: { sheet?: any; element?: HTMLStyleElement } | undefined) {
        if (!entry) return null;
        return entry.element ? entry.element.sheet : entry.sheet;
    }

    butil.cssStyleSheet = {
        isConstructableStyleSheetAvailable() {
            try { return typeof CSSStyleSheet === 'function' && 'replaceSync' in CSSStyleSheet.prototype; }
            catch { return false; }
        },

        // A stylesheet of your own. Constructable sheets are adopted by the document without an
        // element in the markup; where they are missing a <style> element behaves the same from
        // .NET's side.
        createSheet(id: string) {
            if (butil.cssStyleSheet.isConstructableStyleSheetAvailable()) {
                const sheet = new CSSStyleSheet();
                (document as any).adoptedStyleSheets = [...((document as any).adoptedStyleSheets ?? []), sheet];
                _sheets[id] = { sheet };
                return true;
            }

            const element = document.createElement('style');
            document.head.appendChild(element);
            _sheets[id] = { element };
            return true;
        },

        insertRule(id: string, rule: string, index: number) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return -1;
            try {
                return sheet.insertRule(rule, index >= 0 ? index : sheet.cssRules.length);
            } catch {
                // A rule the parser rejects throws rather than being ignored - which is the useful
                // behaviour, and why this answers -1 instead of pretending it worked.
                return -1;
            }
        },

        deleteRule(id: string, index: number) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return false;
            try { sheet.deleteRule(index); return true; } catch { return false; }
        },

        rules(id: string) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return [];
            try { return Array.from(sheet.cssRules).map((rule: any) => rule.cssText); }
            catch { return []; }   // a cross-origin sheet refuses to be read
        },

        replaceSheet(id: string, css: string) {
            const entry = _sheets[id];
            if (!entry) return false;
            try {
                // The element path replaces the sheet rather than editing it, which is exactly why
                // nothing here holds on to the old one.
                if (entry.element) { entry.element.textContent = css; return true; }
                if (typeof entry.sheet?.replaceSync === 'function') { entry.sheet.replaceSync(css); return true; }
                return false;
            } catch { return false; }
        },

        removeSheet(id: string) {
            const entry = _sheets[id];
            if (!entry) return;
            delete _sheets[id];

            if (entry.element) { entry.element.remove(); return; }

            const adopted = ((document as any).adoptedStyleSheets ?? []) as any[];
            (document as any).adoptedStyleSheets = adopted.filter(sheet => sheet !== entry.sheet);
        },

        disposeAll() {
            for (const id of Object.keys(_sheets)) butil.cssStyleSheet.removeSheet(id);
        }
    };
}(BitButil));
