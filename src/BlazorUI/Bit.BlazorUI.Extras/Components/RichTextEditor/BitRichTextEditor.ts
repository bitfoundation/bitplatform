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
                '*': ['class', 'dir'],
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
                    editor._dotNetRef.invokeMethodAsync('OnContentChanged', RichTextEditor.snapshot(editor), RichTextEditor.computeFacts(editor));
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
                const range = sel.getRangeAt(0);
                // Only store a selection that is fully inside this editor: a range that starts
                // inside but ends outside (or vice versa) must not be captured, or a later
                // toolbar action could mutate content beyond the editor.
                if (editor.contains(range.startContainer) && editor.contains(range.endContainer)) {
                    editor._range = range.cloneRange();
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
            return editor ? RichTextEditor.snapshot(editor) : '';
        }

        // Returns the editor content as plain text: visible text only, with block/br boundaries
        // rendered as line breaks (innerText) and non-breaking spaces normalized to regular
        // spaces, matching how the content facts treat text. The transient find-highlight marks
        // only wrap existing text, so they need no special handling here.
        public static getText(editor: any): string {
            if (!editor) return '';
            return (editor.innerText || '').replace(/\u00a0/g, ' ');
        }

        // Extracts the plain text of an arbitrary HTML string (used while source view is active,
        // where the raw-HTML textarea - not the editor DOM - holds the live content).
        public static htmlToText(editor: any, html: string): string {
            if (!html) return '';
            const d = document.createElement('div');
            d.setAttribute('aria-hidden', 'true');
            // Sanitize against the active policy first so markup the editor would never render
            // (e.g. disallowed element bodies) cannot leak into the extracted text.
            d.innerHTML = RichTextEditor.sanitize(editor, html);
            // innerText only honors block/br line breaks on a rendered element; display:none or
            // visibility:hidden would degrade it to textContent and lose the breaks, so park the
            // scratch node offscreen instead.
            d.style.position = 'fixed';
            d.style.top = '0';
            d.style.left = '-99999px';
            document.body.appendChild(d);
            try {
                return (d.innerText || '').replace(/\u00a0/g, ' ');
            } finally {
                d.remove();
            }
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

        // Outbound snapshot sent to .NET (notify/afterChange) or returned to callers (getHtml):
        // first strip the transient find-highlight markup, then sanitize against the active policy
        // (the same enforcement path setHtml/incoming content uses) so persisted Value can never
        // carry markup that bypasses the sanitization allowlist. Transient-mark cleanup stays
        // separate from the policy pass so the two concerns remain independent.
        private static snapshot(editor: any): string {
            if (!editor) return '';
            return RichTextEditor.sanitize(editor, RichTextEditor.cleanHtml(editor));
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
            // The content changed programmatically (e.g. a bound Value assignment), not via a user
            // edit: refresh the cached content facts so count-dependent state stays accurate, but
            // do not route this through the user-change callback (OnContentChanged) or emit an edit.
            if (editor._dotNetRef)
                editor._dotNetRef.invokeMethodAsync('OnFactsChanged', RichTextEditor.computeFacts(editor));
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
        // Scoped to the editor instance (like sanitizeHtml) so validation can honor per-editor
        // options such as the active sanitization policy.
        public static validateHtml(editor: any, html: string): boolean {
            if (!html) return true;

            const voidTags = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr']);
            const optionalClose = new Set(['p', 'li', 'td', 'th', 'tr', 'thead', 'tbody', 'tfoot', 'option', 'optgroup', 'dt', 'dd', 'colgroup', 'col']);
            const nameChar = /[a-zA-Z0-9-]/;

            const stack: string[] = [];
            const len = html.length;
            let i = 0;
            while (i < len) {
                const lt = html.indexOf('<', i);
                // Text up to the next '<' is fine; a stray '>' in text is tolerated as before.
                if (lt === -1) break;

                let j = lt + 1;
                // HTML comments (<!-- ... -->) are valid markup: treat them as inert, skip past
                // the closing '-->', and leave the tag stack untouched. Without this they would be
                // rejected below because '!' is not a tag-name start character.
                if (html[j] === '!' && html[j + 1] === '-' && html[j + 2] === '-') {
                    const end = html.indexOf('-->', j + 3);
                    if (end === -1) return false; // unterminated comment is malformed
                    i = end + 3;
                    continue;
                }
                const isClose = html[j] === '/';
                if (isClose) j++;

                // Tag name must start with a letter; a '<' not opening a real tag is malformed.
                const nameStart = j;
                if (j >= len || !/[a-zA-Z]/.test(html[j])) return false;
                while (j < len && nameChar.test(html[j])) j++;
                const tag = html.slice(nameStart, j).toLowerCase();

                // Scan attributes until the closing '>', tracking quoted state so a '>' inside a
                // single/double-quoted attribute value does not terminate the tag. An unterminated
                // quote (or tag) runs off the end and is rejected as malformed.
                let quote = '';
                let closed = false;
                let selfClose = false;
                while (j < len) {
                    const ch = html[j];
                    if (quote) {
                        if (ch === quote) quote = '';
                    } else if (ch === '"' || ch === "'") {
                        quote = ch;
                    } else if (ch === '>') {
                        selfClose = html[j - 1] === '/';
                        closed = true;
                        j++;
                        break;
                    }
                    j++;
                }
                if (!closed) return false;

                if (isClose) {
                    let matchIndex = -1;
                    for (let k = stack.length - 1; k >= 0; k--) {
                        if (stack[k] === tag) { matchIndex = k; break; }
                    }
                    if (matchIndex === -1) return false;
                    // Anything still open above the match must be an optional-close element.
                    for (let k = matchIndex + 1; k < stack.length; k++) {
                        if (!optionalClose.has(stack[k])) return false;
                    }
                    stack.length = matchIndex;
                } else if (!selfClose && !voidTags.has(tag)) {
                    stack.push(tag);
                }
                i = j;
            }

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
            if (!RichTextEditor.isTagAllowed(editor, 'a') || !RichTextEditor.isAttrAllowed(editor, 'a', 'href')) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'Links are not allowed by the current policy.');
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
            if (!RichTextEditor.isTagAllowed(editor, 'a') || !RichTextEditor.isAttrAllowed(editor, 'a', 'href')) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'Links are not allowed by the current policy.');
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
            if (!RichTextEditor.isTagAllowed(editor, 'img') || !RichTextEditor.isAttrAllowed(editor, 'img', 'src')) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'Images are not allowed by the current policy.');
                return;
            }
            RichTextEditor.dispatch(editor, 'insertImage', { html: `<img src="${RichTextEditor.escapeAttr(url)}" alt="">` });
            RichTextEditor.afterChange(editor);
        }

        public static applyColor(editor: any, kind: string, value: string) {
            if (!editor || !value) return;
            // Color commands emit <span style="..."> (after normalizeFontTags rewrites <font>).
            // If the active policy would strip <span> or the style attribute, snapshot() drops the
            // formatting on the next sanitize roundtrip, so the live editor and persisted Value
            // would diverge. Gate on the allowlist and block with a client error instead.
            if (!RichTextEditor.isTagAllowed(editor, 'span') || !RichTextEditor.isAttrAllowed(editor, 'span', 'style')) {
                RichTextEditor.reportClientError(editor, 'format-not-allowed', 'That formatting is not allowed by the current policy.');
                return;
            }
            RichTextEditor.dispatch(editor, kind === 'back' ? 'backColor' : 'foreColor', { value });
            RichTextEditor.normalizeFontTags(editor);
            RichTextEditor.afterChange(editor);
        }

        public static applyFont(editor: any, kind: string, value: string) {
            if (!editor || !value) return;
            // Font commands emit <span style="..."> (after normalizeFontTags rewrites <font>).
            // Gate on the same allowlist as applyColor so formatting the sanitized snapshot would
            // strip is never applied only to be dropped from the persisted Value.
            if (!RichTextEditor.isTagAllowed(editor, 'span') || !RichTextEditor.isAttrAllowed(editor, 'span', 'style')) {
                RichTextEditor.reportClientError(editor, 'format-not-allowed', 'That formatting is not allowed by the current policy.');
                return;
            }
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

        public static insertMedia(editor: any, html: string): boolean {
            if (!editor || !html) return false;
            // Route media through a media-specific allowlist so only approved embed markup
            // (iframe/video/audio/source with safe attributes and schemes) reaches the document.
            const safe = RichTextEditor.sanitizeMedia(editor, html);
            if (!safe) {
                RichTextEditor.reportClientError(editor, 'media-not-allowed', 'That media could not be embedded.');
                return false;
            }
            RichTextEditor.dispatch(editor, 'insertMedia', { html: safe });
            RichTextEditor.afterChange(editor);
            return true;
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
                // would strip them later, leaving inconsistent state. Deny-by-default: an absent
                // allowedTags is an empty allowlist (deny all), matching sanitize()/isTagAllowed().
                if (!policy || !policy.allowedTags || !policy.allowedTags.includes(tag)) {
                    el.replaceWith(...Array.from(el.childNodes)); return;
                }
                // When the active policy supplies an attribute contract, merge its per-tag and
                // global ('*') allowlists so the media path defers to the policy too. Mirror
                // sanitize()'s deny-by-default semantics: a missing allowedAttributes map is
                // treated as an empty allowlist (deny all) rather than falling back to the
                // hard-coded media allowlist, so a policy that enables media only via allowedTags
                // cannot smuggle hard-coded attributes the policy never permitted.
                const policyAttrs = (policy && policy.allowedAttributes) || {};
                const policyAllowed: string[] = [...(policyAttrs[tag] || []), ...(policyAttrs['*'] || [])];
                for (const attr of Array.from(el.attributes)) {
                    const name = attr.name.toLowerCase();
                    if (name.startsWith('on')) { el.removeAttribute(attr.name); continue; }
                    // Default deny: keep only the per-tag media allowlist or the safe global
                    // attributes; drop anything else regardless of which tag carries it.
                    const allowed = allowedAttrs[tag];
                    const permitted = allowed ? allowed.has(name) : globalAttrs.has(name);
                    // An attribute must clear both the media allowlist and the active policy's
                    // attribute contract so only attributes explicitly permitted by the active
                    // sanitization policy survive.
                    if (!permitted || !policyAllowed.includes(name)) {
                        el.removeAttribute(attr.name); continue;
                    }
                    if (name === 'src') {
                        const val = (attr.value || '').trim();
                        if (tag === 'iframe') {
                            // iframe embeds must clear the active URI policy (custom
                            // allowedUriSchemes included) *and* be HTTPS on the host allowlist; a
                            // non-HTTPS (or unparseable) URL is dropped so mixed-content/downgrade
                            // embeds cannot slip through the media path.
                            let host = '', scheme = '';
                            try { const u = new URL(val); host = u.host.toLowerCase(); scheme = u.protocol.toLowerCase(); } catch { host = ''; scheme = ''; }
                            if (scheme !== 'https:' || !iframeHosts.includes(host) || !RichTextEditor.isAllowedUri(editor, val, false)) { el.remove(); return; }
                        } else if (!RichTextEditor.isAllowedUri(editor, val, false)) {
                            el.removeAttribute(attr.name);
                        }
                    }
                }
            });

            // Source-less embeds would render as blank/broken media and let insertMedia() succeed
            // with an empty embed. After the attribute loop any surviving src is valid (invalid
            // ones were stripped/removed above), so drop any embed that no longer carries a src.
            // Remove src-less <source> first so the video/audio fallback check below is accurate.
            tpl.content.querySelectorAll('source').forEach((el: Element) => {
                if (!el.hasAttribute('src')) el.remove();
            });
            tpl.content.querySelectorAll('iframe').forEach((el: Element) => {
                if (!el.hasAttribute('src')) el.remove();
            });
            // video/audio may supply their src via a <source> child instead of a src attribute,
            // so only drop them when they have neither.
            tpl.content.querySelectorAll('video, audio').forEach((el: Element) => {
                if (!el.hasAttribute('src') && !el.querySelector('source[src]')) el.remove();
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
            // Honor the active sanitization policy before injecting markup: if the policy would
            // strip the table tags during the sanitize pass that feeds the persisted Value, the
            // live editor and the saved Value would diverge (table visible, but dropped on save).
            // Mirror the link/image inserts and block the operation with a client error instead.
            const requiredTags = ['table', 'tbody', 'tr', 'td'];
            if (!requiredTags.every(t => RichTextEditor.isTagAllowed(editor, t))) {
                RichTextEditor.reportClientError(editor, 'table-not-allowed', 'Tables are not allowed by the current policy.');
                return;
            }
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
            const cell = RichTextEditor.cellAtSelection(editor) as HTMLTableCellElement | null;
            if (!cell) return;
            const row = cell.parentElement as HTMLTableRowElement;
            const table = cell.closest('table') as HTMLTableElement | null;
            if (!table || !row) return;

            switch (op) {
                case 'addRow': {
                    // Use the logical grid (accounting for col/rowspans) so the inserted row spans
                    // the full table even when the current row contains merged cells.
                    const { rows, grid, colCount } = RichTextEditor.buildTableGrid(table);
                    const ri = rows.indexOf(row);
                    const nr = document.createElement('tr');
                    const extended = new Set<HTMLTableCellElement>();
                    for (let c = 0; c < colCount; c++) {
                        const here = grid[ri] ? (grid[ri][c] || null) : null;
                        const below = (ri + 1 < rows.length && grid[ri + 1]) ? (grid[ri + 1][c] || null) : null;
                        // A cell whose rowspan straddles the insertion boundary is stretched once
                        // instead of getting a fresh neighbor, so the merged region keeps covering
                        // the new row rather than being split by it.
                        if (here && here === below) {
                            if (!extended.has(here)) { extended.add(here); here.rowSpan = (here.rowSpan || 1) + 1; }
                            continue;
                        }
                        const td = document.createElement('td'); td.innerHTML = '<br>'; nr.appendChild(td);
                    }
                    row.after(nr);
                    break;
                }
                case 'addCol': {
                    const { rows, grid } = RichTextEditor.buildTableGrid(table);
                    const targetCol = RichTextEditor.logicalColumnOf(grid, cell);
                    if (targetCol < 0) break;
                    const insertCol = targetCol + 1;
                    const widened = new Set<HTMLTableCellElement>();
                    for (let r = 0; r < rows.length; r++) {
                        const before = grid[r][targetCol] || null;
                        const at = grid[r][insertCol] || null;
                        // A single cell whose colspan straddles the insertion boundary is widened
                        // once instead of receiving a new neighbor.
                        if (before && before === at) {
                            if (!widened.has(before)) { widened.add(before); before.colSpan = (before.colSpan || 1) + 1; }
                            continue;
                        }
                        const td = document.createElement('td'); td.innerHTML = '<br>';
                        if (at && at.parentElement === rows[r]) at.before(td);
                        else if (before && before.parentElement === rows[r]) before.after(td);
                        else rows[r].appendChild(td);
                    }
                    break;
                }
                case 'delRow': {
                    const { rows, grid, colCount } = RichTextEditor.buildTableGrid(table);
                    if (rows.length <= 1) { table.remove(); break; }
                    const ri = rows.indexOf(row);
                    const nextRow = rows[ri + 1] || null;
                    const handled = new Set<HTMLTableCellElement>();
                    // Walk the logical columns of the row being deleted so spanning cells stay
                    // consistent instead of leaving a row short or dropping a merged region.
                    for (let c = 0; c < colCount; c++) {
                        const cell = grid[ri] ? (grid[ri][c] || null) : null;
                        if (!cell || handled.has(cell)) continue;
                        handled.add(cell);
                        const rowSpan = cell.rowSpan || 1;
                        if (cell.parentElement === row) {
                            // Cell originates in the deleted row. If it spans further down, relocate
                            // it (shrunk by one) into the next row so the region below survives.
                            if (rowSpan > 1 && nextRow) {
                                cell.rowSpan = rowSpan - 1;
                                let ref: HTMLTableCellElement | null = null;
                                for (let k = c + 1; k < colCount; k++) {
                                    const cand = grid[ri + 1] ? (grid[ri + 1][k] || null) : null;
                                    if (cand && cand.parentElement === nextRow) { ref = cand; break; }
                                }
                                if (ref) nextRow.insertBefore(cell, ref);
                                else nextRow.appendChild(cell);
                            }
                            // Otherwise the cell is confined to this row and is removed with it.
                        } else if (rowSpan > 1) {
                            // Cell starts above and spans into the deleted row: shrink its rowspan.
                            cell.rowSpan = rowSpan - 1;
                        }
                    }
                    row.remove();
                    break;
                }
                case 'delCol': {
                    const { rows, grid, colCount } = RichTextEditor.buildTableGrid(table);
                    // A table with a single logical column collapses to nothing once that column
                    // is removed, so drop the whole table. Counting logical columns (not DOM
                    // children) keeps this correct when cells are merged.
                    if (colCount <= 1) { table.remove(); break; }
                    const targetCol = RichTextEditor.logicalColumnOf(grid, cell);
                    if (targetCol < 0) break;
                    const seen = new Set<HTMLTableCellElement>();
                    for (let r = 0; r < rows.length; r++) {
                        const c = grid[r][targetCol];
                        if (!c || seen.has(c)) continue;
                        seen.add(c);
                        const cs = c.colSpan || 1;
                        // Shrink a cell that spans the column; remove a cell that occupies it alone.
                        if (cs > 1) c.colSpan = cs - 1;
                        else c.remove();
                    }
                    break;
                }
                case 'merge': {
                    RichTextEditor.mergeSelectedCells(editor, table);
                    break;
                }
            }
            RichTextEditor.afterChange(editor);
        }

        // Builds a logical row x column model of the table that accounts for rowspan/colspan, so
        // column operations target the correct cells even when cells are merged. grid[r][c] holds
        // the cell occupying that logical position (the same cell instance repeats across every
        // column/row it spans). colCount is the widest logical row.
        private static buildTableGrid(table: HTMLTableElement): { rows: HTMLTableRowElement[], grid: (HTMLTableCellElement | null)[][], colCount: number } {
            // Only this table's own rows: querySelectorAll('tr') also descends into nested
            // tables, so filter to rows whose nearest table is this one to keep nested-table
            // rows/cells out of the outer grid.
            const rows = (Array.from(table.querySelectorAll('tr')) as HTMLTableRowElement[])
                .filter(tr => tr.closest('table') === table);
            const grid: (HTMLTableCellElement | null)[][] = rows.map(() => []);
            for (let r = 0; r < rows.length; r++) {
                let col = 0;
                for (const child of Array.from(rows[r].children)) {
                    const c = child as HTMLTableCellElement;
                    if (c.tagName !== 'TD' && c.tagName !== 'TH') continue;
                    while (grid[r][col]) col++;
                    const colspan = Math.max(1, parseInt(c.getAttribute('colspan') || '1') || 1);
                    const rowspan = Math.max(1, parseInt(c.getAttribute('rowspan') || '1') || 1);
                    for (let dr = 0; dr < rowspan && r + dr < rows.length; dr++) {
                        for (let dc = 0; dc < colspan; dc++) grid[r + dr][col + dc] = c;
                    }
                    col += colspan;
                }
            }
            const colCount = grid.reduce((max, gr) => Math.max(max, gr.length), 0);
            return { rows, grid, colCount };
        }

        // Returns the first logical column index occupied by the given cell, or -1 if not found.
        private static logicalColumnOf(grid: (HTMLTableCellElement | null)[][], cell: HTMLTableCellElement): number {
            for (let r = 0; r < grid.length; r++) {
                const idx = grid[r].indexOf(cell);
                if (idx !== -1) return idx;
            }
            return -1;
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
            // Budget the replacement against the remaining visible-text capacity so a replace
            // cannot push textContent past _maxLength. The matched text is removed, so it frees
            // its own length back into the budget.
            let repl = replacement ?? '';
            const max = editor._maxLength;
            if (max != null) {
                const current = (editor.textContent || '').length;
                const markLen = (mark.textContent || '').length;
                const allowed = Math.max(0, max - (current - markLen));
                if (repl.length > allowed) repl = repl.slice(0, allowed);
            }
            mark.replaceWith(document.createTextNode(repl));
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
            // Track remaining visible-text capacity so cumulative replacements never exceed
            // _maxLength. Each match frees its own length (it is removed) and the inserted
            // replacement consumes from the budget; once exhausted, replacements are trimmed.
            const max = editor._maxLength;
            let remaining = max == null ? Infinity : Math.max(0, max - (editor.textContent || '').length);
            const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT, null);
            const textNodes: Node[] = [];
            while (walker.nextNode()) textNodes.push(walker.currentNode);
            for (const tn of textNodes) {
                const replaced = (tn.nodeValue || '').replace(rx, (matched: string) => {
                    count++;
                    let r = replacement ?? '';
                    if (max != null) {
                        const allowedLen = matched.length + remaining;
                        if (r.length > allowedLen) r = r.slice(0, Math.max(0, allowedLen));
                        remaining += matched.length - r.length;
                    }
                    return r;
                });
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

        // Suppresses the browser default for the slash menu's navigation keys on the filter input
        // so ArrowUp/ArrowDown don't move the text caret, Enter doesn't submit, and Escape doesn't
        // clear the field - while the C# @onkeydown handler still runs and normal typing is left
        // untouched. Bound once per input element (which Blazor recreates each time the menu opens,
        // so no explicit teardown is needed).
        public static bindSlashKeys(input: any) {
            if (!input || input._slashKeysBound) return;
            input._slashKeysBound = true;
            input.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'Enter' || e.key === 'Escape') {
                    e.preventDefault();
                }
            });
        }
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
                // Exclude cells that belong to a nested table so an outer merge never pulls in
                // descendant cells from a table inside one of these cells.
                .filter(c => c.closest('table') === table && range.intersectsNode(c));
            if (selected.length < 2) return;

            // Resolve each selected cell's position from the logical table grid (which accounts
            // for existing rowspan/colspan) rather than DOM child order, so merges stay correct
            // even when the table already contains merged cells. Each cell's extent also includes
            // its current spans so the merged rectangle fully covers previously merged cells.
            const { grid } = RichTextEditor.buildTableGrid(table);
            let minRow = Infinity, maxRow = -Infinity, minCol = Infinity, maxCol = -Infinity;
            const info = selected.map(cell => {
                let rowIdx = -1, colIdx = -1;
                for (let r = 0; r < grid.length && rowIdx < 0; r++) {
                    const c = grid[r].indexOf(cell as HTMLTableCellElement);
                    if (c !== -1) { rowIdx = r; colIdx = c; }
                }
                const colspan = Math.max(1, parseInt(cell.getAttribute('colspan') || '1') || 1);
                const rowspan = Math.max(1, parseInt(cell.getAttribute('rowspan') || '1') || 1);
                const rowEnd = rowIdx + rowspan - 1;
                const colEnd = colIdx + colspan - 1;
                if (rowIdx >= 0) {
                    if (rowIdx < minRow) minRow = rowIdx;
                    if (rowEnd > maxRow) maxRow = rowEnd;
                    if (colIdx < minCol) minCol = colIdx;
                    if (colEnd > maxCol) maxCol = colEnd;
                }
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
                RichTextEditor.handleImageFiles(editor, imageFiles);
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
            // built-in editing keys is kept as a baseline when no combo list was provided, but
            // only for non-Alt combos: treating ctrl+alt (AltGr) presses as owned would block
            // legitimate text entry, so Alt-modified combos are only owned via _shortcutKeys.
            const parts: string[] = ['ctrl'];
            if (e.shiftKey) parts.push('shift');
            if (e.altKey) parts.push('alt');
            parts.push(key);
            const combo = parts.join('+');
            const owned = (editor._shortcutKeys && editor._shortcutKeys.has(combo))
                || (!e.altKey && ['b', 'i', 'u', 'z', 'y'].includes(key));
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
            editor._dotNetRef.invokeMethodAsync('OnContentChanged', RichTextEditor.snapshot(editor), RichTextEditor.computeFacts(editor));
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
            // Guard against a stored range whose endpoints have drifted outside the editor (e.g.
            // the DOM changed since capture) so restored toolbar actions only ever operate on a
            // selection fully contained within this editor.
            if (!editor.contains(r.startContainer) || !editor.contains(r.endContainer)) return;
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
                // Deny-by-default per the allowlist contract: a tag survives only when the active
                // policy explicitly lists it. An absent allowedTags is treated as an empty
                // allowlist (deny all), not allow-all, so a policy that omits it cannot smuggle
                // arbitrary tags through. DEFAULT_POLICY always defines allowedTags.
                if (!policy || !policy.allowedTags || !policy.allowedTags.includes(tag)) {
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
                    // Default to a deny-all allowlist when the policy omits allowedAttributes so a
                    // custom policy without that map cannot let arbitrary (non-event) attributes
                    // survive on otherwise-allowed tags. Merge tag-specific and global ('*')
                    // attribute allowlists so global attributes (style/class/dir) are honored even
                    // when a tag has its own entry - the previous `[tag] || ['*']` form dropped '*'.
                    const allowedAttributes = (policy && policy.allowedAttributes) || {};
                    const allowed = [
                        ...(allowedAttributes[tag] || []),
                        ...(allowedAttributes['*'] || [])
                    ];
                    if (!allowed.includes(name)) el.removeAttribute(attr.name);
                }
                // Harden anchors that survive sanitization with target="_blank": a blank target
                // gives the opened page access to window.opener unless rel includes noopener.
                // Only add rel when the active policy permits it; otherwise drop target="_blank"
                // rather than smuggling an unlisted rel attribute through (which would violate the
                // "only listed attributes survive" guarantee).
                if (tag === 'a' && (el.getAttribute('target') || '').toLowerCase() === '_blank') {
                    const allowedAttributes = (policy && policy.allowedAttributes) || {};
                    const anchorAllowed = [
                        ...(allowedAttributes['a'] || []),
                        ...(allowedAttributes['*'] || [])
                    ];
                    if (anchorAllowed.includes('rel')) {
                        el.setAttribute('rel', 'noopener noreferrer');
                    } else {
                        el.removeAttribute('target');
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
            // Escape ampersands first so that entity-based payloads (e.g. "java&colon;script")
            // cannot survive validation and later decode back into an active scheme inside the
            // inserted markup. Escaping & before the other characters also avoids corrupting the
            // entities this method itself introduces.
            return (s ?? '')
                .replace(/&/g, '&amp;')
                .replace(/"/g, '&quot;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
        }

        // Whether the active policy (or the secure default when none is set) permits a given tag.
        // Command handlers consult this before mutating the live DOM so an insert whose element
        // the sanitized snapshot would strip (e.g. <img>/<a> under a policy that omits the tag)
        // never appears to succeed in the editor while being dropped from the persisted Value.
        private static isTagAllowed(editor: any, tag: string): boolean {
            const policy = (editor && editor._policy) || RichTextEditor.DEFAULT_POLICY;
            // Deny-by-default per the allowlist contract: an absent allowedTags is an empty
            // allowlist (deny all), not allow-all, matching sanitize()'s tag filtering.
            return !!(policy && policy.allowedTags && policy.allowedTags.includes(tag));
        }

        // Whether the active policy permits a given attribute on a tag, merging the tag-specific
        // and global ('*') attribute allowlists exactly as sanitize() does. Command handlers use
        // this (alongside isTagAllowed) so formatting whose markup the sanitized snapshot would
        // strip is never applied to the live DOM only to be dropped from the persisted Value.
        private static isAttrAllowed(editor: any, tag: string, attr: string): boolean {
            const policy = (editor && editor._policy) || RichTextEditor.DEFAULT_POLICY;
            const attrs = (policy && policy.allowedAttributes) || {};
            const allowed = [...(attrs[tag] || []), ...(attrs['*'] || [])];
            return allowed.includes(attr);
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
                // No scheme: relative URL. Reject protocol-relative (//host), the backslash
                // network-path forms (\host / \\host), and the mixed "/\" form - all of which
                // browsers normalize to a protocol-relative //host.
                return !candidate.startsWith('//') && !candidate.startsWith('\\') && !candidate.startsWith('/\\');
            }

            const scheme = schemeMatch[1].toLowerCase();
            if (scheme === 'data') {
                if (!isImage) return false;
                const isImageData = /^data:image\//i.test(candidate);
                if (policy) return policy.allowDataImageUris === true && isImageData;
                return isImageData;
            }

            // The scheme is already lowercased above; lowercase the policy entries too so the
            // JS scheme check matches the C# policy's case-insensitive comparison.
            if (policy && Array.isArray(policy.allowedUriSchemes)) {
                return policy.allowedUriSchemes.some((s: string) => (s || '').toLowerCase() === scheme);
            }
            return ['http', 'https', 'mailto', 'tel'].includes(scheme);
        }

        private static escapeRegExp(s: string): string {
            return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
    }
}
