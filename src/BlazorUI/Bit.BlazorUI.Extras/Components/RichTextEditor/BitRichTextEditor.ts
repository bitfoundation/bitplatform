namespace BitBlazorUI {

    // BitRichTextEditor - thin JS bridge.
    // Owns nothing but DOM events, formatting commands, and selection. All component
    // logic lives in C#. Every formatting/insertion operation flows through `dispatch`,
    // which delegates to the execCommand engine (isolated in one place so it can later be
    // replaced by a Selection/Range engine without touching the C# call sites).
    export class RichTextEditor {

        private static readonly IMAGE_MIME = ['image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/svg+xml'];
        private static readonly MAX_IMAGE_BYTES = 10 * 1024 * 1024;

        // Built-in secure default allowlist, mirroring BitRichTextEditorSanitizationPolicy.Default.
        // Applied when no custom policy is supplied so the no-policy path still enforces an
        // explicit allowlist (tags/attributes/schemes) rather than a small denylist. iframe is
        // intentionally excluded; iframe embeds are opt-in via a custom policy.
        private static readonly DEFAULT_POLICY = {
            allowedTags: [
                'p', 'br', 'span', 'div',
                'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
                'strong', 'b', 'em', 'i', 'u', 's', 'strike', 'sub', 'sup',
                'ul', 'ol', 'li',
                'blockquote', 'pre', 'code',
                'a', 'img', 'hr',
                'table', 'thead', 'tbody', 'tr', 'th', 'td',
                'audio', 'video', 'source'
            ],
            allowedAttributes: {
                '*': ['style', 'class', 'dir'],
                'a': ['href', 'title', 'target', 'rel'],
                'img': ['src', 'alt', 'width', 'height'],
                'td': ['colspan', 'rowspan'],
                'th': ['colspan', 'rowspan'],
                'audio': ['src', 'controls'],
                'video': ['src', 'controls', 'width', 'height'],
                'source': ['src', 'type']
            } as { [tag: string]: string[] },
            allowedUriSchemes: ['http', 'https', 'mailto', 'tel'],
            allowDataImageUris: true
        };

        // ====================================================================
        // Lifecycle
        // ====================================================================
        public static initialize(editor: any, dotnetObj: DotNetObject, options: any) {
            if (!editor) return;
            options = options || {};
            editor._dotNetRef = dotnetObj;
            RichTextEditor.updateOptions(editor, options);
            let timer: ReturnType<typeof setTimeout> | null = null;

            const notify = () => {
                RichTextEditor.updateEmpty(editor);
                if (editor._dotNetRef)
                    editor._dotNetRef.invokeMethodAsync('OnContentChanged', RichTextEditor.cleanHtml(editor), RichTextEditor.computeFacts(editor));
            };
            editor._notify = notify;

            editor._onInput = () => {
                RichTextEditor.updateEmpty(editor);
                if (timer) clearTimeout(timer);
                timer = setTimeout(notify, editor._debounce);
            };
            editor.addEventListener('input', editor._onInput);

            editor._onBlur = () => {
                if (timer) { clearTimeout(timer); timer = null; }
                notify();
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnBlurred');
            };
            editor.addEventListener('blur', editor._onBlur);

            editor._onFocus = () => {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnFocused');
            };
            editor.addEventListener('focus', editor._onFocus);

            editor._onSelection = () => {
                const sel = document.getSelection();
                if (!sel || sel.rangeCount === 0) return;
                if (editor.contains(sel.anchorNode)) {
                    editor._range = sel.getRangeAt(0).cloneRange();
                    RichTextEditor.reportState(editor);
                }
            };
            document.addEventListener('selectionchange', editor._onSelection);

            // Report browser full-screen changes (including exits via Escape or browser UI) so
            // the component's _fullScreen state never drifts from the actual view.
            editor._onFullScreenChange = () => {
                const root = editor.closest('.bit-rte');
                const isFs = !!document.fullscreenElement && document.fullscreenElement === root;
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnFullScreenChanged', isFs);
            };
            document.addEventListener('fullscreenchange', editor._onFullScreenChange);

            editor._onPaste = (e: ClipboardEvent) => RichTextEditor.onPaste(editor, e);
            editor.addEventListener('paste', editor._onPaste);

            editor._onDrop = (e: DragEvent) => RichTextEditor.onDrop(editor, e);
            editor.addEventListener('drop', editor._onDrop);

            editor._onKeyDown = (e: KeyboardEvent) => RichTextEditor.onKeyDown(editor, e);
            editor.addEventListener('keydown', editor._onKeyDown);

            editor._onBeforeInput = (e: InputEvent) => RichTextEditor.onBeforeInput(editor, e);
            editor.addEventListener('beforeinput', editor._onBeforeInput);

            editor._onInputMd = (e: InputEvent) => RichTextEditor.onInputMarkdown(editor, e);
            editor.addEventListener('input', editor._onInputMd);

            RichTextEditor.enableImageResize(editor);
            RichTextEditor.enableTableResize(editor);
            RichTextEditor.updateEmpty(editor);
        }

        // Refreshes the bridge options that can change after initialization (debounce, policy,
        // upload availability, paste mode, max length, owned shortcut combos) without rebinding
        // the DOM event listeners. Called on first setup and whenever the C# parameters change.
        public static updateOptions(editor: any, options: any) {
            if (!editor) return;
            options = options || {};
            editor._debounce = options.debounce ?? 200;
            editor._policy = options.policy ?? null;
            editor._hasUpload = options.hasUpload === true;
            editor._plainTextPaste = options.plainTextPaste === true;
            editor._maxLength = (typeof options.maxLength === 'number') ? options.maxLength : null;
            editor._shortcutKeys = new Set((Array.isArray(options.shortcutKeys) ? options.shortcutKeys : [])
                .map((k: string) => (k || '').toLowerCase()));
        }

        public static dispose(editor: any) {
            if (!editor) return;
            editor.removeEventListener('input', editor._onInput);
            editor.removeEventListener('input', editor._onInputMd);
            editor.removeEventListener('blur', editor._onBlur);
            editor.removeEventListener('focus', editor._onFocus);
            editor.removeEventListener('paste', editor._onPaste);
            editor.removeEventListener('drop', editor._onDrop);
            editor.removeEventListener('keydown', editor._onKeyDown);
            editor.removeEventListener('beforeinput', editor._onBeforeInput);
            document.removeEventListener('selectionchange', editor._onSelection);
            document.removeEventListener('fullscreenchange', editor._onFullScreenChange);
            RichTextEditor.removeResizeHandle(editor);
            editor._dotNetRef = null;
            editor._range = null;
        }

        // ====================================================================
        // Content get/set
        // ====================================================================
        public static getHtml(editor: any): string {
            return editor ? RichTextEditor.cleanHtml(editor) : '';
        }

        // Returns the editor's HTML with transient find-highlight markup stripped, so the
        // temporary <mark class="bit-rte-find"> nodes never leak into persisted Value.
        private static cleanHtml(editor: any): string {
            if (!editor) return '';
            if (!editor.querySelector('mark.bit-rte-find')) return editor.innerHTML;
            const clone = editor.cloneNode(true) as HTMLElement;
            clone.querySelectorAll('mark.bit-rte-find').forEach((m: Element) => {
                m.replaceWith(...Array.from(m.childNodes));
            });
            clone.normalize();
            return clone.innerHTML;
        }

        // Undo-safe set: when the surface is focused and already has content, route the
        // replacement through the engine (insertHTML) so the native undo stack survives.
        public static setHtml(editor: any, html: string) {
            if (!editor) return;
            // Always sanitize inbound HTML against the active policy (or the secure default
            // when no policy is set) before it reaches the DOM.
            const next = RichTextEditor.sanitize(editor, html ?? '');
            if (editor.innerHTML === next) return;

            const focused = document.activeElement === editor;
            const hasContent = editor.innerHTML.trim().length > 0;
            if (focused && hasContent) {
                const sel = document.getSelection();
                const range = document.createRange();
                range.selectNodeContents(editor);
                sel!.removeAllRanges();
                sel!.addRange(range);
                if (!RichTextEditor.execNative(editor, 'insertHTML', next)) {
                    editor.innerHTML = next;
                }
            } else {
                editor.innerHTML = next;
            }
            RichTextEditor.updateEmpty(editor);
        }

        public static focus(editor: any) {
            editor?.focus();
        }

        // Sanitize an arbitrary HTML string against the active policy (used by source-view exit).
        public static sanitizeHtml(editor: any, html: string): string {
            return RichTextEditor.sanitize(editor, html ?? '');
        }

        // Real (tag-stack) HTML validation used by the source-view exit path. Returns false for
        // stray angle brackets, unmatched closing tags, or misnested/unclosed elements so
        // malformed markup is rejected before it is committed. Void elements and tags with
        // optional end tags (p, li, td, ...) are handled leniently to match the HTML spec.
        public static validateHtml(html: string): boolean {
            if (!html) return true;

            const voidTags = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr']);
            const optionalClose = new Set(['p', 'li', 'td', 'th', 'tr', 'thead', 'tbody', 'tfoot', 'option', 'optgroup', 'dt', 'dd', 'colgroup', 'col']);
            const tagRx = /<\/?([a-zA-Z][a-zA-Z0-9-]*)([^>]*?)(\/?)>/g;

            const stack: string[] = [];
            let lastIndex = 0;
            let m: RegExpExecArray | null;
            while ((m = tagRx.exec(html)) !== null) {
                // Any stray '<' in the text between tags means malformed markup.
                if (html.slice(lastIndex, m.index).indexOf('<') !== -1) return false;
                lastIndex = tagRx.lastIndex;

                const tag = m[1].toLowerCase();
                const isClose = m[0][1] === '/';
                const selfClose = m[3] === '/';

                if (isClose) {
                    let matchIndex = -1;
                    for (let j = stack.length - 1; j >= 0; j--) {
                        if (stack[j] === tag) { matchIndex = j; break; }
                    }
                    if (matchIndex === -1) return false;
                    // Anything still open above the match must be an optional-close element.
                    for (let j = matchIndex + 1; j < stack.length; j++) {
                        if (!optionalClose.has(stack[j])) return false;
                    }
                    stack.length = matchIndex;
                } else if (!selfClose && !voidTags.has(tag)) {
                    stack.push(tag);
                }
            }
            if (html.slice(lastIndex).indexOf('<') !== -1) return false;

            // Leftover open tags are only acceptable if they have optional end tags.
            return stack.every(t => optionalClose.has(t));
        }

        // ====================================================================
        // Command entry points used by C# (all route through dispatch)
        // ====================================================================
        public static exec(editor: any, command: string, value?: string): string {
            if (!editor) return '';
            RichTextEditor.dispatch(editor, command, { value });
            RichTextEditor.afterChange(editor);
            return editor.innerHTML;
        }

        public static execBlock(editor: any, tag: string): string {
            if (!editor) return '';
            RichTextEditor.dispatch(editor, 'formatBlock', { value: tag });
            RichTextEditor.afterChange(editor);
            return editor.innerHTML;
        }

        public static createLink(editor: any, url: string) {
            if (!editor || !url) return;
            if (!RichTextEditor.isAllowedUri(editor, url, false)) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'That link URL is not allowed.');
                return;
            }
            RichTextEditor.dispatch(editor, 'createLink', { value: url });
            RichTextEditor.afterChange(editor);
        }

        public static updateLink(editor: any, url: string) {
            if (!editor || !url) return;
            if (!RichTextEditor.isAllowedUri(editor, url, false)) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'That link URL is not allowed.');
                return;
            }
            // Restore the editor's saved range first so the link is applied to the editor
            // selection rather than whatever the toolbar/dialog interaction left active.
            RichTextEditor.restoreSelection(editor);
            const a = RichTextEditor.linkAtSelection(editor);
            if (a) {
                a.setAttribute('href', url);
            } else {
                RichTextEditor.dispatch(editor, 'createLink', { value: url });
            }
            RichTextEditor.afterChange(editor);
        }

        public static insertImageUrl(editor: any, url: string) {
            if (!editor || !url) return;
            if (!RichTextEditor.isAllowedUri(editor, url, true)) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'That image URL is not allowed.');
                return;
            }
            RichTextEditor.dispatch(editor, 'insertImage', { html: `<img src="${RichTextEditor.escapeAttr(url)}" alt="">` });
            RichTextEditor.afterChange(editor);
        }

        public static applyColor(editor: any, kind: string, value: string) {
            if (!editor || !value) return;
            RichTextEditor.dispatch(editor, kind === 'back' ? 'backColor' : 'foreColor', { value });
            RichTextEditor.normalizeFontTags(editor);
            RichTextEditor.afterChange(editor);
        }

        public static applyFont(editor: any, kind: string, value: string) {
            if (!editor || !value) return;
            RichTextEditor.dispatch(editor, kind === 'size' ? 'fontSize' : 'fontName', { value });
            RichTextEditor.normalizeFontTags(editor);
            RichTextEditor.afterChange(editor);
        }

        // execCommand emits <font> elements (color/face) which the sanitizer allowlist drops
        // because <font> is not a permitted tag - taking the formatting with them on the next
        // sanitize roundtrip (paste, setHtml, source view). Rewrite them into allowed
        // <span style="..."> wrappers so the font formatting survives.
        private static normalizeFontTags(editor: any) {
            if (!editor) return;
            editor.querySelectorAll('font').forEach((f: HTMLElement) => {
                const span = document.createElement('span');
                if (f.style.cssText) span.style.cssText = f.style.cssText;
                const color = f.getAttribute('color');
                const face = f.getAttribute('face');
                if (color) span.style.color = color;
                if (face) span.style.fontFamily = face;
                while (f.firstChild) span.appendChild(f.firstChild);
                f.replaceWith(span);
            });
        }

        public static insertMedia(editor: any, html: string) {
            if (!editor || !html) return;
            // Route media through a media-specific allowlist so only approved embed markup
            // (iframe/video/audio/source with safe attributes and schemes) reaches the document.
            const safe = RichTextEditor.sanitizeMedia(editor, html);
            if (!safe) {
                RichTextEditor.reportClientError(editor, 'media-not-allowed', 'That media could not be embedded.');
                return;
            }
            RichTextEditor.dispatch(editor, 'insertMedia', { html: safe });
            RichTextEditor.afterChange(editor);
        }

        // Media-specific allowlist: permits only the embed elements/attributes produced by the
        // server-side media builder, strips event handlers, and validates src schemes/hosts.
        private static sanitizeMedia(editor: any, html: string): string {
            const tpl = document.createElement('template');
            tpl.innerHTML = html;
            const policy = (editor && editor._policy) || RichTextEditor.DEFAULT_POLICY;
            const allowedTags = new Set(['iframe', 'video', 'audio', 'source', 'br', 'p']);
            const allowedAttrs: { [tag: string]: Set<string> } = {
                iframe: new Set(['src', 'width', 'height', 'allow', 'allowfullscreen', 'frameborder']),
                video: new Set(['src', 'controls', 'width', 'height']),
                audio: new Set(['src', 'controls']),
                source: new Set(['src', 'type'])
            };
            // Global attributes permitted on any allowed tag (e.g. wrapper p/br). Everything else
            // is denied by default so non-media tags cannot smuggle arbitrary attributes through.
            const globalAttrs = new Set(['class', 'dir']);
            const iframeHosts = ['www.youtube-nocookie.com', 'youtube-nocookie.com', 'www.youtube.com', 'youtube.com', 'player.vimeo.com'];

            tpl.content.querySelectorAll('*').forEach((el: Element) => {
                const tag = el.tagName.toLowerCase();
                if (!allowedTags.has(tag)) { el.replaceWith(...Array.from(el.childNodes)); return; }
                // Honor the active sanitization policy first: media tags (notably iframe, which
                // is opt-in) are only permitted when the policy allows them; otherwise setHtml()
                // would strip them later, leaving inconsistent state.
                if (policy && policy.allowedTags && !policy.allowedTags.includes(tag)) {
                    el.replaceWith(...Array.from(el.childNodes)); return;
                }
                for (const attr of Array.from(el.attributes)) {
                    const name = attr.name.toLowerCase();
                    if (name.startsWith('on')) { el.removeAttribute(attr.name); continue; }
                    // Default deny: keep only the per-tag media allowlist or the safe global
                    // attributes; drop anything else regardless of which tag carries it.
                    const allowed = allowedAttrs[tag];
                    const permitted = allowed ? allowed.has(name) : globalAttrs.has(name);
                    if (!permitted) { el.removeAttribute(attr.name); continue; }
                    if (name === 'src') {
                        const val = (attr.value || '').trim();
                        if (tag === 'iframe') {
                            // iframe embeds must be HTTPS *and* on the host allowlist; a non-HTTPS
                            // (or unparseable) URL is dropped so mixed-content/downgrade embeds
                            // cannot slip through the media path.
                            let host = '', scheme = '';
                            try { const u = new URL(val); host = u.host.toLowerCase(); scheme = u.protocol.toLowerCase(); } catch { host = ''; scheme = ''; }
                            if (scheme !== 'https:' || !iframeHosts.includes(host)) { el.remove(); return; }
                        } else if (!RichTextEditor.isAllowedUri(editor, val, false)) {
                            el.removeAttribute(attr.name);
                        }
                    }
                }
            });
            return tpl.innerHTML;
        }

        public static insertText(editor: any, text: string) {
            if (!editor || !text) return;
            // Restore the editor's saved range so the insert (and the budget calculation below)
            // targets the editor's actual selection rather than whatever the live document
            // selection is after a toolbar/custom-item interaction.
            RichTextEditor.restoreSelection(editor);
            // Honor the same _maxLength budget enforced by onBeforeInput/paste so programmatic
            // inserts (emoji picker, custom toolbar items) cannot push past the limit.
            const max = editor._maxLength;
            if (max != null) {
                const sel = document.getSelection();
                const selected = (sel && !sel.isCollapsed) ? sel.toString().length : 0;
                const current = (editor.textContent || '').length;
                const remaining = Math.max(0, max - (current - selected));
                if (remaining === 0) return;
                if (text.length > remaining) text = text.slice(0, remaining);
            }
            RichTextEditor.dispatch(editor, 'insertText', { value: text });
            RichTextEditor.afterChange(editor);
        }

        public static insertTable(editor: any, rows: number, cols: number) {
            if (!editor) return;
            let html = '<table class="bit-rte-table"><tbody>';
            for (let r = 0; r < rows; r++) {
                html += '<tr>';
                for (let c = 0; c < cols; c++) html += '<td><br></td>';
                html += '</tr>';
            }
            html += '</tbody></table><p><br></p>';
            RichTextEditor.dispatch(editor, 'insertHtml', { html });
            RichTextEditor.afterChange(editor);
        }

        public static tableOp(editor: any, op: string) {
            // Restore the editor selection so the operation targets the cell the user last
            // selected in the editor, not a selection left in the toolbar.
            RichTextEditor.restoreSelection(editor);
            const cell = RichTextEditor.cellAtSelection(editor);
            if (!cell) return;
            const row = cell.parentElement as HTMLTableRowElement;
            const table = cell.closest('table');
            if (!table || !row) return;
            const colIndex = Array.from(row.children).indexOf(cell);

            switch (op) {
                case 'addRow': {
                    const nr = document.createElement('tr');
                    for (let i = 0; i < row.children.length; i++) {
                        const td = document.createElement('td'); td.innerHTML = '<br>'; nr.appendChild(td);
                    }
                    row.after(nr);
                    break;
                }
                case 'addCol': {
                    for (const tr of Array.from(table.querySelectorAll('tr'))) {
                        const td = document.createElement('td'); td.innerHTML = '<br>';
                        const ref = tr.children[colIndex];
                        if (ref) ref.after(td); else tr.appendChild(td);
                    }
                    break;
                }
                case 'delRow': {
                    const rows = table.querySelectorAll('tr');
                    if (rows.length <= 1) { table.remove(); } else { row.remove(); }
                    break;
                }
                case 'delCol': {
                    const firstRow = table.querySelector('tr');
                    if (firstRow && firstRow.children.length <= 1) { table.remove(); }
                    else { for (const tr of Array.from(table.querySelectorAll('tr'))) { const c = tr.children[colIndex]; if (c) c.remove(); } }
                    break;
                }
                case 'merge': {
                    RichTextEditor.mergeSelectedCells(editor, table);
                    break;
                }
            }
            RichTextEditor.afterChange(editor);
        }

        // ---- find & replace ----
        public static clearFind(editor: any) {
            if (!editor) return;
            editor.querySelectorAll('mark.bit-rte-find').forEach((m: HTMLElement) => {
                const parent = m.parentNode;
                m.replaceWith(...Array.from(m.childNodes));
                parent && parent.normalize();
            });
            editor._findIndex = -1;
        }

        public static find(editor: any, term: string, caseSensitive: boolean): number {
            RichTextEditor.clearFind(editor);
            if (!term) return 0;
            const flags = caseSensitive ? 'g' : 'gi';
            const rx = new RegExp(RichTextEditor.escapeRegExp(term), flags);
            let count = 0;
            const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT, null);
            const textNodes: Node[] = [];
            while (walker.nextNode()) textNodes.push(walker.currentNode);
            for (const tn of textNodes) {
                const text = tn.nodeValue || '';
                if (!rx.test(text)) continue;
                rx.lastIndex = 0;
                const frag = document.createDocumentFragment();
                let last = 0, m: RegExpExecArray | null;
                while ((m = rx.exec(text)) !== null) {
                    if (m.index > last) frag.appendChild(document.createTextNode(text.slice(last, m.index)));
                    const mark = document.createElement('mark');
                    mark.className = 'bit-rte-find';
                    mark.textContent = m[0];
                    frag.appendChild(mark);
                    last = m.index + m[0].length;
                    count++;
                    if (m[0].length === 0) rx.lastIndex++;
                }
                if (last < text.length) frag.appendChild(document.createTextNode(text.slice(last)));
                (tn as ChildNode).replaceWith(frag);
            }
            editor._findIndex = count > 0 ? 0 : -1;
            return count;
        }

        public static replaceCurrent(editor: any, term: string, replacement: string, caseSensitive: boolean): number {
            const marks = editor.querySelectorAll('mark.bit-rte-find');
            if (marks.length === 0) return 0;
            const idx = Math.min(Math.max(editor._findIndex ?? 0, 0), marks.length - 1);
            const mark = marks[idx];
            mark.replaceWith(document.createTextNode(replacement ?? ''));
            editor.normalize();
            RichTextEditor.afterChange(editor);
            return RichTextEditor.find(editor, term, caseSensitive);
        }

        public static replaceAll(editor: any, term: string, replacement: string, caseSensitive: boolean): number {
            RichTextEditor.clearFind(editor);
            if (!term) return 0;
            const flags = caseSensitive ? 'g' : 'gi';
            const rx = new RegExp(RichTextEditor.escapeRegExp(term), flags);
            let count = 0;
            const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT, null);
            const textNodes: Node[] = [];
            while (walker.nextNode()) textNodes.push(walker.currentNode);
            for (const tn of textNodes) {
                const replaced = (tn.nodeValue || '').replace(rx, () => { count++; return replacement ?? ''; });
                if (replaced !== tn.nodeValue) tn.nodeValue = replaced;
            }
            RichTextEditor.afterChange(editor);
            return count;
        }

        // ---- full screen / direction ----
        public static setFullScreen(editor: any, on: boolean) {
            if (!editor) return;
            const root = editor.closest('.bit-rte');
            if (!root) return;
            if (on) {
                if (root.requestFullscreen) {
                    // Return the promise so the C# interop await (and ToggleFullScreen) only
                    // proceeds once the request settles. Report denial via OnClientError, but
                    // re-throw so the awaiting caller still observes the failure rather than a
                    // silently-resolved promise that looks like success.
                    return root.requestFullscreen().catch((err: any) => {
                        if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnClientError', 'fullscreen-denied', 'Full-screen mode was blocked by the browser.');
                        throw err;
                    });
                }
            } else if (document.fullscreenElement) {
                return document.exitFullscreen?.();
            }
        }

        public static setBlockDirection(editor: any, dir: string) {
            // Restore the editor's saved range so the direction is applied to the editor's
            // block rather than a selection left active in the toolbar/dialog.
            RichTextEditor.restoreSelection(editor);
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnClientError', 'no-selection', 'Select a block to change its direction.');
                return;
            }
            // Reject selections that are not inside this editor so external DOM cannot be
            // modified through the restored/live selection.
            if (!sel.anchorNode || !editor.contains(sel.anchorNode)) {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnClientError', 'no-selection', 'Select a block to change its direction.');
                return;
            }
            let node: Node | null = sel.anchorNode;
            if (node && node.nodeType === 3) node = node.parentNode;
            let block: any = node;
            while (block && block !== editor && getComputedStyle(block).display === 'inline') block = block.parentNode;
            if (block && block !== editor) {
                block.setAttribute('dir', dir);
                RichTextEditor.afterChange(editor);
            }
        }

        // ---- toolbar roving tabindex ----
        public static enableToolbarRoving(toolbar: any) {
            if (!toolbar || toolbar._roving) return;
            toolbar._roving = true;
            // Only enabled interactive controls join the roving tab order. Disabled
            // buttons/inputs/selects and non-focusable <label> wrappers are excluded so keyboard
            // navigation never traps on an item that can't take focus.
            const items = () => ([...toolbar.querySelectorAll('button,select,input')] as HTMLElement[])
                .filter(el => !(el as HTMLButtonElement | HTMLInputElement | HTMLSelectElement).disabled);
            const setTabs = (activeIdx: number) => {
                const list = items();
                list.forEach((el, i) => el.tabIndex = i === activeIdx ? 0 : -1);
            };
            setTabs(0);
            toolbar.addEventListener('keydown', (e: KeyboardEvent) => {
                const list = items();
                let idx = list.indexOf(document.activeElement as HTMLElement);
                if (idx < 0) return;
                if (e.key === 'ArrowRight') { e.preventDefault(); idx = (idx + 1) % list.length; }
                else if (e.key === 'ArrowLeft') { e.preventDefault(); idx = (idx - 1 + list.length) % list.length; }
                else if (e.key === 'Home') { e.preventDefault(); idx = 0; }
                else if (e.key === 'End') { e.preventDefault(); idx = list.length - 1; }
                else return;
                setTabs(idx);
                list[idx].focus();
            });
            toolbar.addEventListener('focusin', (e: FocusEvent) => {
                const list = items();
                const idx = list.indexOf(e.target as HTMLElement);
                if (idx >= 0) setTabs(idx);
            });
        }

        // Removes the leading "/" trigger then applies a slash-menu command.
        public static applySlashCommand(editor: any, command: string) {
            // Restore the editor's saved range first so focus is back inside the editor and the
            // slash block lookup targets the real caret position rather than a stale selection.
            RichTextEditor.restoreSelection(editor);
            const block = RichTextEditor.currentBlock(editor);
            if (block && (block.textContent || '').startsWith('/')) {
                block.textContent = block.textContent!.slice(1);
            }
            if (['h1', 'h2', 'h3', 'p', 'blockquote', 'pre'].includes(command)) {
                RichTextEditor.dispatch(editor, 'formatBlock', { value: command });
            } else {
                RichTextEditor.dispatch(editor, command, {});
            }
            RichTextEditor.afterChange(editor);
        }

        // ====================================================================
        // Engine: the ONLY place document.execCommand is invoked.
        // ====================================================================
        private static dispatch(editor: any, command: string, args: any): boolean {
            if (!editor) return false;
            try {
                return RichTextEditor.engineRun(editor, command, args || {});
            } catch (err: any) {
                if (editor._dotNetRef) {
                    editor._dotNetRef.invokeMethodAsync('OnCommandError', String(command), String(err?.message ?? err));
                }
                return false;
            }
        }

        private static engineRun(editor: any, command: string, args: any): boolean {
            editor.focus();
            RichTextEditor.restoreSelection(editor);
            try { document.execCommand('styleWithCSS', false, 'false'); } catch { /* ignore */ }

            switch (command) {
                case 'formatBlock': {
                    let v = args?.value ?? 'p';
                    if (v && v[0] !== '<') v = '<' + v + '>';
                    return RichTextEditor.execNative(editor, 'formatBlock', v);
                }
                case 'foreColor':
                    return RichTextEditor.execNative(editor, 'foreColor', args?.value);
                case 'backColor':
                    return RichTextEditor.execNative(editor, 'hiliteColor', args?.value) ||
                        RichTextEditor.execNative(editor, 'backColor', args?.value);
                case 'fontName':
                    return RichTextEditor.execNative(editor, 'fontName', args?.value);
                case 'fontSize':
                    return RichTextEditor.applyFontSize(editor, args?.value);
                case 'insertImage':
                    return RichTextEditor.insertNodeHtml(editor, args?.html);
                case 'insertHtml':
                    return RichTextEditor.execNative(editor, 'insertHTML', args?.html);
                case 'insertHorizontalRule':
                    return RichTextEditor.insertHorizontalRule(editor);
                case 'createLink':
                    return RichTextEditor.createLinkImpl(editor, args?.value);
                case 'insertTable':
                    return RichTextEditor.insertNodeHtml(editor, args?.html);
                case 'insertMedia':
                    return RichTextEditor.insertNodeHtml(editor, args?.html);
                default:
                    return RichTextEditor.execNative(editor, command, args?.value ?? null);
            }
        }

        private static execNative(editor: any, command: string, value?: any): boolean {
            try { return document.execCommand(command, false, value ?? undefined); }
            catch { return false; }
        }

        // Normalize execCommand fontSize (1-7) onto a real size by rewriting the produced
        // <font size> into an inline style when a css length is supplied.
        private static applyFontSize(editor: any, value: string): boolean {
            if (!value) return false;
            RichTextEditor.execNative(editor, 'fontSize', '7');
            editor.querySelectorAll('font[size="7"]').forEach((f: HTMLElement) => {
                f.removeAttribute('size');
                f.style.fontSize = value;
            });
            return true;
        }

        // ====================================================================
        // Markdown shortcuts + slash trigger
        // ====================================================================
        private static onInputMarkdown(editor: any, e: InputEvent) {
            if (editor._mdBusy) return;
            const block = RichTextEditor.currentBlock(editor);
            if (!block) return;
            const text = block.textContent || '';

            if (e.inputType === 'insertText' && e.data === '/' && text === '/') {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnSlashTrigger');
                return;
            }

            if (e.inputType !== 'insertText' || e.data !== ' ') return;
            const map: { [key: string]: string } = {
                '#': 'h1', '##': 'h2', '###': 'h3',
                '>': 'blockquote'
            };
            const marker = text.trim();
            if (map[marker]) {
                editor._mdBusy = true;
                RichTextEditor.clearBlockText(block);
                RichTextEditor.dispatch(editor, 'formatBlock', { value: map[marker] });
                editor._mdBusy = false;
                RichTextEditor.afterChange(editor);
            } else if (marker === '-' || marker === '*') {
                editor._mdBusy = true;
                RichTextEditor.clearBlockText(block);
                RichTextEditor.dispatch(editor, 'insertUnorderedList', {});
                editor._mdBusy = false;
                RichTextEditor.afterChange(editor);
            } else if (marker === '1.') {
                editor._mdBusy = true;
                RichTextEditor.clearBlockText(block);
                RichTextEditor.dispatch(editor, 'insertOrderedList', {});
                editor._mdBusy = false;
                RichTextEditor.afterChange(editor);
            }
        }

        private static currentBlock(editor: any): HTMLElement | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            if (node && node.nodeType === 3) node = node.parentNode;
            while (node && node !== editor && getComputedStyle(node).display === 'inline') node = node.parentNode;
            return node && node !== editor ? node : null;
        }

        private static clearBlockText(block: HTMLElement) {
            block.textContent = '';
            const sel = document.getSelection();
            const range = document.createRange();
            range.selectNodeContents(block);
            range.collapse(true);
            sel!.removeAllRanges();
            sel!.addRange(range);
        }

        // ====================================================================
        // Tables / image helpers
        // ====================================================================
        private static enableTableResize(editor: any) {
            if (editor._tableResizeWired) return;
            editor._tableResizeWired = true;
            editor.addEventListener('mousedown', (e: MouseEvent) => {
                const target = e.target as HTMLElement;
                const cell = target.closest && target.closest('td,th') as HTMLElement;
                if (!cell) return;
                const rect = cell.getBoundingClientRect();
                if (e.clientX < rect.right - 6) return;
                e.preventDefault();
                const startX = e.clientX;
                const startW = rect.width;
                const onMove = (m: MouseEvent) => {
                    const w = Math.max(1, Math.round(startW + (m.clientX - startX)));
                    cell.style.width = `${w}px`;
                };
                const onUp = () => {
                    document.removeEventListener('mousemove', onMove);
                    document.removeEventListener('mouseup', onUp);
                    const w = Math.max(1, Math.round(cell.getBoundingClientRect().width));
                    cell.setAttribute('width', String(w));
                    if (editor._notify) editor._notify();
                };
                document.addEventListener('mousemove', onMove);
                document.addEventListener('mouseup', onUp);
            });
        }

        private static cellAtSelection(editor: any): HTMLElement | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            while (node && node !== editor) {
                if (node.nodeType === 1 && (node.tagName === 'TD' || node.tagName === 'TH')) return node;
                node = node.parentNode;
            }
            return null;
        }

        private static mergeSelectedCells(editor: any, table: HTMLTableElement) {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return;
            const range = sel.getRangeAt(0);
            const selected = (Array.from(table.querySelectorAll('td,th')) as HTMLElement[])
                .filter(c => range.intersectsNode(c));
            if (selected.length < 2) return;

            const rows = Array.from(table.querySelectorAll('tr')) as HTMLTableRowElement[];

            // Map each selected cell to its (row, column) position so the merge can span the
            // full selected rectangle rather than collapsing everything onto a single row.
            let minRow = Infinity, maxRow = -Infinity, minCol = Infinity, maxCol = -Infinity;
            const info = selected.map(cell => {
                const tr = cell.parentElement as HTMLTableRowElement;
                const rowIdx = rows.indexOf(tr);
                const colIdx = Array.from(tr.children).indexOf(cell);
                if (rowIdx < minRow) minRow = rowIdx;
                if (rowIdx > maxRow) maxRow = rowIdx;
                if (colIdx < minCol) minCol = colIdx;
                if (colIdx > maxCol) maxCol = colIdx;
                return { cell, rowIdx, colIdx };
            });

            const topLeft = info.find(i => i.rowIdx === minRow && i.colIdx === minCol)?.cell;
            if (!topLeft) return;

            const colspan = maxCol - minCol + 1;
            const rowspan = maxRow - minRow + 1;

            for (const { cell } of info) {
                if (cell === topLeft) continue;
                if (cell.innerHTML && cell.innerHTML !== '<br>') topLeft.innerHTML += ' ' + cell.innerHTML;
                cell.remove();
            }

            if (colspan > 1) topLeft.setAttribute('colspan', String(colspan)); else topLeft.removeAttribute('colspan');
            if (rowspan > 1) topLeft.setAttribute('rowspan', String(rowspan)); else topLeft.removeAttribute('rowspan');
        }

        private static enableImageResize(editor: any) {
            if (!editor || editor._resizeWired) return;
            editor._resizeWired = true;
            editor.addEventListener('click', (e: MouseEvent) => {
                const target = e.target as HTMLElement;
                if (target && target.tagName === 'IMG') RichTextEditor.startImageResize(editor, target as HTMLImageElement);
                else RichTextEditor.removeResizeHandle(editor);
            });
        }

        private static startImageResize(editor: any, img: HTMLImageElement) {
            RichTextEditor.removeResizeHandle(editor);
            const handle = document.createElement('span');
            handle.className = 'bit-rte-resize-handle';
            handle.contentEditable = 'false';
            Object.assign(handle.style, {
                position: 'absolute', width: '12px', height: '12px',
                background: '#0969da', border: '2px solid #fff', borderRadius: '2px',
                cursor: 'nwse-resize', zIndex: '5'
            });
            document.body.appendChild(handle);
            editor._resizeHandle = handle;

            const place = () => {
                const r = img.getBoundingClientRect();
                handle.style.left = `${window.scrollX + r.right - 6}px`;
                handle.style.top = `${window.scrollY + r.bottom - 6}px`;
            };
            place();
            editor._resizeReposition = place;
            window.addEventListener('scroll', place, true);

            handle.addEventListener('mousedown', (e: MouseEvent) => {
                e.preventDefault();
                const startX = e.clientX;
                const startW = img.getBoundingClientRect().width;
                const maxW = editor.clientWidth;
                const onMove = (m: MouseEvent) => {
                    let w = Math.round(startW + (m.clientX - startX));
                    w = Math.max(16, Math.min(w, maxW));
                    img.style.width = `${w}px`;
                    place();
                };
                const onUp = () => {
                    document.removeEventListener('mousemove', onMove);
                    document.removeEventListener('mouseup', onUp);
                    const finalW = Math.max(16, Math.min(Math.round(img.getBoundingClientRect().width), editor.clientWidth));
                    img.setAttribute('width', String(finalW));
                    img.style.width = `${finalW}px`;
                    if (editor._notify) editor._notify();
                };
                document.addEventListener('mousemove', onMove);
                document.addEventListener('mouseup', onUp);
            });
        }

        private static removeResizeHandle(editor: any) {
            if (editor._resizeHandle) {
                editor._resizeHandle.remove();
                editor._resizeHandle = null;
            }
            if (editor._resizeReposition) {
                window.removeEventListener('scroll', editor._resizeReposition, true);
                editor._resizeReposition = null;
            }
        }

        private static async handleImageFiles(editor: any, files: File[]) {
            let accepted = 0;
            for (const file of files) {
                if (accepted >= 20) {
                    RichTextEditor.reportClientError(editor, 'too-many-files', 'Only 20 images can be inserted per drop.');
                    break;
                }
                if (!RichTextEditor.IMAGE_MIME.includes(file.type)) {
                    RichTextEditor.reportClientError(editor, 'invalid-file', `"${file.name}" is not a supported image type.`);
                    continue;
                }
                if (file.size > RichTextEditor.MAX_IMAGE_BYTES) {
                    RichTextEditor.reportClientError(editor, 'file-too-large', `"${file.name}" exceeds the 10 MB limit.`);
                    continue;
                }
                accepted++;
                try {
                    const dataUrl = await RichTextEditor.readAsDataUrl(file);
                    let url: string | null = dataUrl;
                    if (editor._hasUpload && editor._dotNetRef) {
                        const base64 = (dataUrl.split(',')[1]) ?? '';
                        url = await editor._dotNetRef.invokeMethodAsync('ResolveImageUrl', file.name, file.type, base64);
                        if (!url) continue;
                    }
                    // Enforce the active URI policy on the final image source (raw data URL or the
                    // resolved upload URL) so disallowed data URIs / schemes are not inserted.
                    if (!RichTextEditor.isAllowedUri(editor, url, true)) {
                        RichTextEditor.reportClientError(editor, 'invalid-image-uri', `"${file.name}" has a disallowed image source.`);
                        continue;
                    }
                    RichTextEditor.dispatch(editor, 'insertImage', { html: `<img src="${RichTextEditor.escapeAttr(url)}" alt="${RichTextEditor.escapeAttr(file.name)}">` });
                } catch {
                    // Fail this file only; keep processing the rest of the batch.
                    RichTextEditor.reportClientError(editor, 'image-read-failed', `"${file.name}" could not be processed.`);
                    continue;
                }
            }
            if (editor._notify) editor._notify();
        }

        private static readAsDataUrl(file: File): Promise<string> {
            return new Promise((resolve, reject) => {
                const fr = new FileReader();
                fr.onload = () => resolve(fr.result as string);
                fr.onerror = () => reject(fr.error);
                fr.readAsDataURL(file);
            });
        }

        private static reportClientError(editor: any, code: string, message: string) {
            if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnClientError', code, message);
        }

        // ====================================================================
        // Events
        // ====================================================================
        private static onPaste(editor: any, e: ClipboardEvent) {
            const cb = e.clipboardData;
            if (!cb) return;

            const imageFiles = Array.from<DataTransferItem>(cb.items as any || [])
                .filter((it: DataTransferItem) => it.kind === 'file' && it.type.startsWith('image/'))
                .map((it: DataTransferItem) => it.getAsFile())
                .filter(Boolean) as File[];
            if (imageFiles.length > 0) {
                e.preventDefault();
                RichTextEditor.handleImageFiles(editor, imageFiles);
                return;
            }

            e.preventDefault();
            const html = cb.getData('text/html');
            const text = cb.getData('text/plain');
            RichTextEditor.insertTransferContent(editor, html, text);
        }

        // Shared sanitized-insertion path for both paste and drop: HTML is sanitized (with Word
        // normalization) unless plain-text mode is on, plain text is escaped, and the result is
        // clamped to the _maxLength budget before being dispatched.
        private static insertTransferContent(editor: any, html: string, text: string) {
            const plainOnly = editor._plainTextPaste === true;
            let toInsert = (!plainOnly && html)
                ? RichTextEditor.sanitize(editor, RichTextEditor.normalizeWordHtml(html))
                : RichTextEditor.escapeHtml(text).replace(/\r?\n/g, '<br>');

            const max = editor._maxLength;
            if (max != null) {
                // Selected text will be replaced by the insert, so it counts against neither
                // the current length nor the remaining budget.
                const sel = document.getSelection();
                const selected = (sel && !sel.isCollapsed) ? sel.toString().length : 0;
                const current = (editor.textContent || '').length;
                const remaining = Math.max(0, max - (current - selected));
                if (remaining === 0) return;
                // Measure the final inserted content (sanitized HTML, HTML-only, or escaped
                // plain text) and truncate that markup so it cannot exceed the remaining budget,
                // rather than budgeting against the plain-text payload which may differ from
                // toInsert (or be empty for HTML-only transfers).
                if (RichTextEditor.visibleTextLength(toInsert) > remaining) {
                    toInsert = RichTextEditor.truncateHtmlToVisibleLength(toInsert, remaining);
                }
            }
            RichTextEditor.dispatch(editor, 'insertHtml', { html: toInsert });
            if (editor._notify) editor._notify();
        }

        private static onDrop(editor: any, e: DragEvent) {
            const dt = e.dataTransfer;
            if (!dt) return;
            const imageFiles = Array.from<File>(dt.files as any || []).filter((f: File) => f.type.startsWith('image/')) as File[];
            if (imageFiles.length > 0) {
                e.preventDefault();
                RichTextEditor.placeDropCaret(editor, e);
                RichTextEditor.handleImageFiles(editor, Array.from<File>(dt.files as any));
                return;
            }

            // Non-image drops (text/html, text/plain) are routed through the same sanitized
            // insertion path as paste so dropped markup cannot bypass sanitize()/the max-length
            // budget via the browser's default contenteditable handling.
            const html = dt.getData('text/html');
            const text = dt.getData('text/plain');
            if (!html && !text) return;
            e.preventDefault();
            RichTextEditor.placeDropCaret(editor, e);
            RichTextEditor.insertTransferContent(editor, html, text);
        }

        // Move the editor selection (and the saved range) to the drop point so the subsequent
        // insert targets where the user dropped rather than the prior caret position.
        private static placeDropCaret(editor: any, e: DragEvent) {
            const range = RichTextEditor.caretRangeFromPoint(e.clientX, e.clientY);
            if (range) {
                const sel = document.getSelection();
                sel!.removeAllRanges();
                sel!.addRange(range);
                editor._range = range.cloneRange();
            }
        }

        private static caretRangeFromPoint(x: number, y: number): Range | null {
            const doc = document as any;
            if (doc.caretRangeFromPoint) return doc.caretRangeFromPoint(x, y);
            if (doc.caretPositionFromPoint) {
                const p = doc.caretPositionFromPoint(x, y);
                if (p) { const r = document.createRange(); r.setStart(p.offsetNode, p.offset); r.collapse(true); return r; }
            }
            return null;
        }

        private static async onKeyDown(editor: any, e: KeyboardEvent) {
            if (!(e.ctrlKey || e.metaKey)) return;
            const key = e.key.toLowerCase();
            const primary = e.ctrlKey || e.metaKey;

            // Identify owned shortcuts synchronously (before any await) so the browser default
            // never wins the race against the async .NET dispatch. The combo is built to match
            // the C# BuildComboKey form ("ctrl+b", "ctrl+shift+z", ...). The hardcoded set of
            // built-in editing keys is kept as a baseline when no combo list was provided.
            const parts: string[] = ['ctrl'];
            if (e.shiftKey) parts.push('shift');
            if (e.altKey) parts.push('alt');
            parts.push(key);
            const combo = parts.join('+');
            const owned = (editor._shortcutKeys && editor._shortcutKeys.has(combo))
                || ['b', 'i', 'u', 'z', 'y'].includes(key);
            if (owned) e.preventDefault();

            if (!editor._dotNetRef) return;
            const handled = await editor._dotNetRef.invokeMethodAsync('OnShortcut', key, primary, e.shiftKey, e.altKey);
            // For non-owned combos the .NET side may still report custom handling; suppress the
            // default in that case too (best-effort, since the await has already yielded).
            if (handled && !owned) e.preventDefault();
        }

        private static onBeforeInput(editor: any, e: InputEvent) {
            const max = editor._maxLength;
            if (max == null) return;
            const current = (editor.textContent || '').length;

            const isInsert = e.inputType && e.inputType.startsWith('insert');
            if (!isInsert) return;
            if (e.inputType === 'insertFromPaste') return;

            // Account for any selected text that will be replaced so in-place edits at the
            // limit are allowed when the net length does not increase.
            const sel = document.getSelection();
            const selected = (sel && !sel.isCollapsed) ? sel.toString().length : 0;
            const adding = (e.data ? e.data.length : 1);
            if (current - selected + adding > max) {
                e.preventDefault();
            }
        }

        // ====================================================================
        // Selection state + content facts
        // ====================================================================
        private static afterChange(editor: any) {
            RichTextEditor.updateEmpty(editor);
            if (!editor._dotNetRef) return;
            editor._dotNetRef.invokeMethodAsync('OnContentChanged', RichTextEditor.cleanHtml(editor), RichTextEditor.computeFacts(editor));
            RichTextEditor.reportState(editor);
        }

        // Toggles the placeholder (empty) class synchronously so the placeholder shows/hides
        // instantly while typing, independent of the debounced .NET content notification.
        private static updateEmpty(editor: any) {
            if (!editor) return;
            const hasText = (editor.textContent || '').replace(/\u00a0/g, ' ').trim().length > 0;
            const hasEmbedded = !!editor.querySelector('img,table,hr,audio,video,iframe');
            editor.classList.toggle('bit-rte-edt-empty', !hasText && !hasEmbedded);
        }

        private static reportState(editor: any) {
            if (!editor._dotNetRef) return;
            editor._dotNetRef.invokeMethodAsync('OnSelectionChanged', RichTextEditor.currentState(editor));
        }

        private static currentState(editor: any): any {
            const q = (c: string) => { try { return document.queryCommandState(c); } catch { return false; } };
            const v = (c: string) => { try { return (document.queryCommandValue(c) || '').toString(); } catch { return ''; } };
            let block = '';
            try { block = (document.queryCommandValue('formatBlock') || '').toString().toLowerCase(); } catch { /* ignore */ }

            const link = RichTextEditor.linkAtSelection(editor);
            return {
                bold: q('bold'),
                italic: q('italic'),
                underline: q('underline'),
                strikeThrough: q('strikeThrough'),
                orderedList: q('insertOrderedList'),
                unorderedList: q('insertUnorderedList'),
                justifyLeft: q('justifyLeft'),
                justifyCenter: q('justifyCenter'),
                justifyRight: q('justifyRight'),
                block: block,
                subscript: q('subscript'),
                superscript: q('superscript'),
                foreColor: v('foreColor') || null,
                backColor: v('backColor') || null,
                fontName: (v('fontName') || '').replace(/^['"]|['"]$/g, '') || null,
                fontSize: v('fontSize') || null,
                direction: RichTextEditor.directionAtSelection(editor),
                inLink: !!link,
                linkHref: link ? link.getAttribute('href') : null
            };
        }

        private static computeFacts(editor: any): any {
            const text = (editor.textContent || '').replace(/\u00a0/g, ' ');
            const hasText = text.trim().length > 0;
            const hasEmbedded = !!editor.querySelector('img,table,hr,audio,video,iframe');
            const chars = text.replace(/\s+$/g, '').length === 0 && !hasText ? 0 : text.length;
            const words = (text.trim().match(/\S+/g) || []).length;
            return {
                hasText: hasText,
                hasEmbeddedContent: hasEmbedded,
                characterCount: hasText ? text.length : (chars),
                wordCount: words
            };
        }

        // ====================================================================
        // Helpers
        // ====================================================================
        private static linkAtSelection(editor: any): HTMLElement | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            while (node && node !== editor) {
                if (node.nodeType === 1 && node.tagName === 'A') return node;
                node = node.parentNode;
            }
            return null;
        }

        private static directionAtSelection(editor: any): string | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            if (node && node.nodeType === 3) node = node.parentNode;
            while (node && node !== editor) {
                if (node.nodeType === 1 && node.dir) return node.dir;
                node = node.parentNode;
            }
            return null;
        }

        private static insertNodeHtml(editor: any, html: string): boolean {
            if (!html) return false;
            return RichTextEditor.execNative(editor, 'insertHTML', html);
        }

        private static insertHorizontalRule(editor: any): boolean {
            if (!RichTextEditor.execNative(editor, 'insertHorizontalRule')) {
                return RichTextEditor.execNative(editor, 'insertHTML', '<hr>');
            }
            return true;
        }

        private static createLinkImpl(editor: any, url: string): boolean {
            if (!url) return false;
            const sel = document.getSelection();
            if (sel && sel.isCollapsed) {
                return RichTextEditor.execNative(editor, 'insertHTML',
                    `<a href="${RichTextEditor.escapeAttr(url)}">${RichTextEditor.escapeHtml(url)}</a>`);
            }
            return RichTextEditor.execNative(editor, 'createLink', url);
        }

        private static restoreSelection(editor: any) {
            const r = editor._range;
            if (!r) return;
            const sel = document.getSelection();
            if (!sel) return;
            sel.removeAllRanges();
            sel.addRange(r);
        }

        // Allowlist-aware sanitize. A custom policy (editor._policy) is applied when present;
        // otherwise the built-in secure DEFAULT_POLICY allowlist is enforced. Either way only
        // listed tags/attributes survive, so non-URI attributes like formaction are dropped
        // unless explicitly allowed, and event handlers / disallowed URI schemes are stripped.
        private static sanitize(editor: any, html: string): string {
            const tpl = document.createElement('template');
            tpl.innerHTML = html;
            const policy = (editor && editor._policy) || RichTextEditor.DEFAULT_POLICY;

            tpl.content.querySelectorAll('script,style,iframe,object,embed,link,meta,title,head').forEach((n: Element) => {
                if (policy && policy.allowedTags && policy.allowedTags.includes(n.tagName.toLowerCase())) return;
                n.remove();
            });

            tpl.content.querySelectorAll('*').forEach((el: Element) => {
                const tag = el.tagName.toLowerCase();
                if (policy && policy.allowedTags && !policy.allowedTags.includes(tag)) {
                    el.replaceWith(...Array.from(el.childNodes));
                    return;
                }
                for (const attr of Array.from(el.attributes)) {
                    const name = attr.name.toLowerCase();
                    const val = attr.value;
                    if (name.startsWith('on')) { el.removeAttribute(attr.name); continue; }
                    if (name === 'href' || name === 'src') {
                        // Enforce the active policy's scheme allowlist on every inbound HTML
                        // path (paste, source import, setHtml) - not just the command handlers.
                        const isImageSrc = name === 'src' && tag === 'img';
                        if (!RichTextEditor.isAllowedUri(editor, val, isImageSrc)) {
                            el.removeAttribute(attr.name); continue;
                        }
                    }
                    if (policy && policy.allowedAttributes) {
                        // Merge tag-specific and global ('*') attribute allowlists so global
                        // attributes (style/class/dir) are honored even when a tag has its own
                        // entry - the previous `[tag] || ['*']` form dropped the '*' set.
                        const allowed = [
                            ...(policy.allowedAttributes[tag] || []),
                            ...(policy.allowedAttributes['*'] || [])
                        ];
                        if (!allowed.includes(name)) el.removeAttribute(attr.name);
                    }
                }
            });
            return tpl.innerHTML;
        }

        private static normalizeWordHtml(html: string): string {
            return html
                .replace(/<!--[\s\S]*?-->/g, '')
                .replace(/<\/?o:[^>]*>/gi, '')
                .replace(/<\/?w:[^>]*>/gi, '')
                .replace(/\s(class|style)="[^"]*mso[^"]*"/gi, '');
        }

        private static escapeHtml(s: string): string {
            const d = document.createElement('div');
            d.textContent = s ?? '';
            return d.innerHTML;
        }

        // Measures the visible (text) length of an HTML fragment, matching how _maxLength is
        // enforced against the editor's textContent length.
        private static visibleTextLength(html: string): number {
            const d = document.createElement('div');
            d.innerHTML = html ?? '';
            return (d.textContent || '').length;
        }

        // Truncates an HTML fragment so its visible text length does not exceed max, walking
        // text nodes and dropping any content past the budget while preserving surrounding markup.
        private static truncateHtmlToVisibleLength(html: string, max: number): string {
            const d = document.createElement('div');
            d.innerHTML = html ?? '';
            let remaining = max;
            const walker = document.createTreeWalker(d, NodeFilter.SHOW_TEXT);
            const toRemove: Node[] = [];
            let node: Node | null;
            while ((node = walker.nextNode())) {
                const len = (node.textContent || '').length;
                if (remaining <= 0) {
                    toRemove.push(node);
                } else if (len > remaining) {
                    node.textContent = (node.textContent || '').slice(0, remaining);
                    remaining = 0;
                } else {
                    remaining -= len;
                }
            }
            toRemove.forEach(n => { if (n.parentNode) n.parentNode.removeChild(n); });
            return d.innerHTML;
        }

        private static escapeAttr(s: string): string {
            return (s ?? '').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        }

        // Validates a URL against the active sanitization policy's scheme allowlist (or a
        // secure default when no policy is present). Relative URLs are allowed; protocol-
        // relative (//host) and javascript: URLs are rejected. data: is only allowed for
        // images and only when the policy permits it.
        private static isAllowedUri(editor: any, url: string, isImage: boolean): boolean {
            const policy = editor && editor._policy;
            const trimmed = (url || '').trim();
            if (!trimmed) return false;

            // Browsers ignore tab/newline/CR and other control characters when resolving a
            // URL's scheme, so strip them before validating. This defeats obfuscated values
            // like "java\nscript:" or "java\tscript:" that would otherwise dodge the checks.
            const candidate = trimmed.replace(/[\u0000-\u0020\u007F-\u009F\u200B-\u200D\uFEFF]/g, '');
            if (!candidate) return false;
            if (/^javascript:/i.test(candidate)) return false;
            if (/^vbscript:/i.test(candidate)) return false;

            const schemeMatch = /^([a-z][a-z0-9+.-]*):/i.exec(candidate);
            if (!schemeMatch) {
                // No scheme: relative URL. Reject protocol-relative (//host).
                return !candidate.startsWith('//');
            }

            const scheme = schemeMatch[1].toLowerCase();
            if (scheme === 'data') {
                if (!isImage) return false;
                const isImageData = /^data:image\//i.test(candidate);
                if (policy) return policy.allowDataImageUris === true && isImageData;
                return isImageData;
            }

            if (policy && Array.isArray(policy.allowedUriSchemes)) {
                return policy.allowedUriSchemes.includes(scheme);
            }
            return ['http', 'https', 'mailto', 'tel'].includes(scheme);
        }

        private static escapeRegExp(s: string): string {
            return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
    }
}
