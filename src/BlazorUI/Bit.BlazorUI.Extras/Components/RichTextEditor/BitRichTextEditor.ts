namespace BitBlazorUI {

    // BitRichTextEditor - thin JS bridge.
    // Owns nothing but DOM events, formatting commands, and selection. All component
    // logic lives in C#. Every formatting/insertion operation flows through `dispatch`,
    // which delegates to the execCommand engine (isolated in one place so it can later be
    // replaced by a Selection/Range engine without touching the C# call sites).
    export class RichTextEditor {

        private static readonly IMAGE_MIME = ['image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/svg+xml'];

        // The only hosts an <iframe> may point at under the built-in policy. Media embeds are
        // useful enough to be on by default, but only because the sanitize pass host-restricts
        // them: an iframe is the one allowlisted element that can run code from another origin.
        private static readonly IFRAME_HOSTS = [
            'www.youtube-nocookie.com', 'youtube-nocookie.com',
            'www.youtube.com', 'youtube.com',
            'player.vimeo.com'
        ];
        private static readonly MAX_IMAGE_BYTES = 10 * 1024 * 1024;

        // Elements that are never content: they are removed outright (with their subtree) rather
        // than unwrapped, because unwrapping them can re-expose their body as markup on the next
        // parse (the classic mutation-XSS vector) or leave foreign-namespace children behind.
        private static readonly DROPPED_TAGS =
            'script,style,iframe,object,embed,link,meta,title,head,base,form,noscript,template,' +
            'svg,math,frame,frameset,applet,dialog,noembed,xmp,plaintext';

        // CSS declarations kept when a 'style' attribute survives the tag/attribute allowlist.
        // Everything outside this set is dropped, which keeps the formatting the editor itself
        // produces (color, font, alignment, indentation, sizing) while denying layout/behavior
        // properties that could be used to overlay or exfiltrate page content.
        private static readonly ALLOWED_CSS_PROPS = new Set([
            'color', 'background-color', 'background',
            'font-family', 'font-size', 'font-weight', 'font-style', 'font-variant',
            'text-align', 'text-decoration', 'text-decoration-line', 'text-indent', 'text-transform',
            'letter-spacing', 'word-spacing', 'line-height', 'white-space', 'direction', 'unicode-bidi',
            'vertical-align', 'list-style-type', 'list-style-position',
            'margin', 'margin-top', 'margin-right', 'margin-bottom', 'margin-left',
            'padding', 'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
            'width', 'height', 'max-width', 'max-height', 'min-width', 'min-height',
            'border', 'border-top', 'border-right', 'border-bottom', 'border-left',
            'border-color', 'border-style', 'border-width', 'border-radius', 'border-collapse',
            'caption-side', 'table-layout', 'float', 'clear', 'display', 'opacity'
        ]);

        // Values that can smuggle a URL, a script, or a legacy IE behavior into a style
        // declaration. Any declaration whose value matches is dropped.
        private static readonly UNSAFE_CSS_VALUE = /url\s*\(|expression\s*\(|javascript\s*:|vbscript\s*:|behavior\s*:|-moz-binding|@import|\\/i;

        // Built-in secure default allowlist, mirroring BitRichTextEditorSanitizationPolicy.Default.
        // Applied when no custom policy is supplied so the no-policy path still enforces an
        // explicit allowlist (tags/attributes/schemes) rather than a small denylist. iframe is
        // intentionally excluded; iframe embeds are opt-in via a custom policy.
        private static readonly DEFAULT_POLICY = {
            allowedTags: [
                'p', 'br', 'span', 'div',
                'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
                'strong', 'b', 'em', 'i', 'u', 's', 'strike', 'sub', 'sup', 'mark',
                'ul', 'ol', 'li',
                'blockquote', 'pre', 'code',
                'a', 'img', 'hr',
                'table', 'caption', 'colgroup', 'col', 'thead', 'tbody', 'tfoot', 'tr', 'th', 'td',
                'audio', 'video', 'source', 'iframe'
            ],
            allowedAttributes: {
                // 'style' and 'align' carry the output of the formatting commands themselves
                // (colors, fonts, font sizes, alignment, indentation), so they must survive the
                // sanitize pass or the editor would show formatting the persisted value drops.
                // Style values are additionally filtered through a CSS property allowlist.
                '*': ['class', 'dir', 'style', 'align', 'title'],
                'span': ['data-mention-id'],
                'a': ['href', 'title', 'target', 'rel'],
                'img': ['src', 'alt', 'width', 'height', 'loading'],
                'ol': ['start', 'type', 'reversed'],
                'ul': ['type'],
                'li': ['value', 'data-checked'],
                'table': ['border', 'cellpadding', 'cellspacing', 'width', 'height'],
                'col': ['span', 'width'],
                'colgroup': ['span'],
                'td': ['colspan', 'rowspan', 'width', 'height', 'valign', 'headers'],
                'th': ['colspan', 'rowspan', 'width', 'height', 'valign', 'scope', 'abbr'],
                'audio': ['src', 'controls'],
                'video': ['src', 'controls', 'width', 'height', 'poster'],
                'source': ['src', 'type'],
                'iframe': ['src', 'width', 'height', 'allow', 'allowfullscreen', 'frameborder', 'loading', 'referrerpolicy']
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

            // The selection rectangle is reported in component coordinates, so scrolling the
            // surface moves the selection without firing selectionchange. Re-report on scroll (once
            // per frame) so the selection toolbar follows the text instead of drifting off it.
            let scrollFrame = 0;
            editor._onScroll = () => {
                if (!editor._quickToolbar || scrollFrame) return;
                scrollFrame = requestAnimationFrame(() => {
                    scrollFrame = 0;
                    RichTextEditor.reportState(editor);
                });
            };
            editor.addEventListener('scroll', editor._onScroll);

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
            RichTextEditor.enableTaskToggle(editor);
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
            // Mirrors ReadOnly/IsEnabled from C#. The DOM listeners stay bound (so the surface can
            // become editable again without a re-init), but every mutating handler bails on it, so
            // a read-only editor cannot be changed by paste, drop, typing, or a shortcut.
            editor._readOnly = options.readOnly === true;
            editor._autoLink = options.autoLink !== false;
            // Measuring the selection forces layout on every selection change, and the result is
            // only used to place the floating selection toolbar - so it is computed solely when
            // that toolbar is actually enabled.
            editor._quickToolbar = options.quickToolbar === true;
            editor._mentions = options.mentions === true;
            editor._shortcutKeys = new Set((Array.isArray(options.shortcutKeys) ? options.shortcutKeys : [])
                .map((k: string) => (k || '').toLowerCase()));
        }

        public static dispose(editor: any) {
            if (!editor) return;
            editor.removeEventListener('input', editor._onInput);
            editor.removeEventListener('input', editor._onInputMd);
            editor.removeEventListener('blur', editor._onBlur);
            editor.removeEventListener('focus', editor._onFocus);
            editor.removeEventListener('scroll', editor._onScroll);
            editor.removeEventListener('paste', editor._onPaste);
            editor.removeEventListener('drop', editor._onDrop);
            editor.removeEventListener('keydown', editor._onKeyDown);
            editor.removeEventListener('beforeinput', editor._onBeforeInput);
            document.removeEventListener('selectionchange', editor._onSelection);
            document.removeEventListener('fullscreenchange', editor._onFullScreenChange);
            RichTextEditor.removeResizeHandle(editor);
            editor._dotNetRef = null;
            editor._range = null;
            editor._activeImage = null;
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

        public static createLink(editor: any, url: string, newTab?: boolean, text?: string) {
            if (!editor || !url) return;
            if (!RichTextEditor.linkAllowed(editor, url)) return;

            RichTextEditor.restoreSelection(editor);
            const sel = document.getSelection();
            const collapsed = !sel || sel.rangeCount === 0 || sel.isCollapsed;
            const label = (text || '').trim();

            if (collapsed || label) {
                // Nothing is selected (or the caller supplied the link text), so the anchor and its
                // text are inserted as one piece instead of linking whatever happens to be selected.
                const shown = label || url;
                RichTextEditor.dispatch(editor, 'insertHtml', {
                    html: `<a href="${RichTextEditor.escapeAttr(url)}"${RichTextEditor.linkTargetAttrs(editor, newTab)}>${RichTextEditor.escapeHtml(shown)}</a>`
                });
            } else {
                RichTextEditor.dispatch(editor, 'createLink', { value: url });
                if (newTab) RichTextEditor.markSelectedLinksNewTab(editor);
            }
            RichTextEditor.afterChange(editor);
        }

        public static updateLink(editor: any, url: string, newTab?: boolean) {
            if (!editor || !url) return;
            if (!RichTextEditor.linkAllowed(editor, url)) return;
            // Restore the editor's saved range first so the link is applied to the editor
            // selection rather than whatever the toolbar/dialog interaction left active.
            RichTextEditor.restoreSelection(editor);
            const a = RichTextEditor.linkAtSelection(editor);
            if (a) {
                a.setAttribute('href', url);
                RichTextEditor.setLinkTarget(editor, a, newTab === true);
            } else {
                RichTextEditor.dispatch(editor, 'createLink', { value: url });
                if (newTab) RichTextEditor.markSelectedLinksNewTab(editor);
            }
            RichTextEditor.afterChange(editor);
        }

        // Shared precondition for the two link entry points: the URL must clear the active scheme
        // allowlist and the policy must actually permit an anchor with an href.
        private static linkAllowed(editor: any, url: string): boolean {
            if (!RichTextEditor.isAllowedUri(editor, url, false)) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'That link URL is not allowed.');
                return false;
            }
            if (!RichTextEditor.isTagAllowed(editor, 'a') || !RichTextEditor.isAttrAllowed(editor, 'a', 'href')) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'Links are not allowed by the current policy.');
                return false;
            }
            return true;
        }

        // The target/rel pair for a new-tab link, or an empty string when the policy would strip
        // target anyway (the sanitizer removes a blank target it cannot pair with a rel).
        private static linkTargetAttrs(editor: any, newTab?: boolean): string {
            if (!newTab || !RichTextEditor.isAttrAllowed(editor, 'a', 'target')) return '';
            return RichTextEditor.isAttrAllowed(editor, 'a', 'rel')
                ? ' target="_blank" rel="noopener noreferrer"'
                : '';
        }

        // Adds or removes the new-tab behavior on one anchor, always pairing target="_blank" with
        // rel="noopener noreferrer" so the opened page never gets window.opener access.
        private static setLinkTarget(editor: any, a: HTMLElement, newTab: boolean) {
            if (!newTab) {
                a.removeAttribute('target');
                a.removeAttribute('rel');
                return;
            }
            if (!RichTextEditor.isAttrAllowed(editor, 'a', 'target') || !RichTextEditor.isAttrAllowed(editor, 'a', 'rel')) return;
            a.setAttribute('target', '_blank');
            a.setAttribute('rel', 'noopener noreferrer');
        }

        // execCommand('createLink') can produce several anchors across a multi-node selection, so
        // every anchor the selection touches gets the new-tab treatment.
        private static markSelectedLinksNewTab(editor: any) {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return;
            const range = sel.getRangeAt(0);
            (Array.from(editor.querySelectorAll('a[href]')) as HTMLElement[])
                .filter(a => range.intersectsNode(a))
                .forEach(a => RichTextEditor.setLinkTarget(editor, a, true));
        }

        public static insertImageUrl(editor: any, url: string, alt?: string) {
            if (!editor || !url) return;
            if (!RichTextEditor.isAllowedUri(editor, url, true)) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'That image URL is not allowed.');
                return;
            }
            if (!RichTextEditor.isTagAllowed(editor, 'img') || !RichTextEditor.isAttrAllowed(editor, 'img', 'src')) {
                RichTextEditor.reportClientError(editor, 'invalid-url', 'Images are not allowed by the current policy.');
                return;
            }
            RichTextEditor.dispatch(editor, 'insertImage', {
                html: `<img src="${RichTextEditor.escapeAttr(url)}" alt="${RichTextEditor.escapeAttr(alt ?? '')}">`
            });
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
            const iframeHosts = RichTextEditor.IFRAME_HOSTS;

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

        // Inserts arbitrary markup at the caret after running it through the active policy, so an
        // imperative InsertHtmlAsync can never introduce content the sanitizer would strip.
        public static insertHtml(editor: any, html: string) {
            if (!editor || !html) return;
            const safe = RichTextEditor.sanitize(editor, html);
            if (!safe) return;
            RichTextEditor.restoreSelection(editor);
            // Refuse (rather than truncate) an insert that would break the cap: a half-inserted
            // fragment is worse than none, unlike a paste where trimming the tail is expected.
            if (!RichTextEditor.fitsWithinMaxLength(editor, safe)) {
                RichTextEditor.reportClientError(editor, 'max-length', 'The content would exceed the maximum length.');
                return;
            }
            RichTextEditor.dispatch(editor, 'insertHtml', { html: safe });
            RichTextEditor.afterChange(editor);
        }

        // Selects the whole document inside the editor (and records it as the editor's range) so a
        // following command applies to everything.
        public static selectAll(editor: any) {
            if (!editor) return;
            editor.focus();
            const sel = document.getSelection();
            if (!sel) return;
            const range = document.createRange();
            range.selectNodeContents(editor);
            sel.removeAllRanges();
            sel.addRange(range);
            editor._range = range.cloneRange();
            RichTextEditor.reportState(editor);
        }

        // Returns the plain text of the current selection, or an empty string when the selection is
        // collapsed or sits outside this editor.
        public static getSelectedText(editor: any): string {
            if (!editor) return '';
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return '';
            const range = sel.getRangeAt(0);
            if (!editor.contains(range.startContainer) || !editor.contains(range.endContainer)) return '';
            return sel.toString().replace(/\u00a0/g, ' ');
        }

        public static insertTable(editor: any, rows: number, cols: number, header?: boolean) {
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
            let html = '<table class="bit-rte-table">';
            let bodyRows = rows;
            if (header === true && rows > 0 && RichTextEditor.isTagAllowed(editor, 'thead') && RichTextEditor.isTagAllowed(editor, 'th')) {
                html += '<thead><tr>';
                for (let c = 0; c < cols; c++) html += '<th><br></th>';
                html += '</tr></thead>';
                bodyRows = rows - 1;
            }
            html += '<tbody>';
            for (let r = 0; r < bodyRows; r++) {
                html += '<tr>';
                for (let c = 0; c < cols; c++) html += '<td><br></td>';
                html += '</tr>';
            }
            html += '</tbody></table><p><br></p>';
            RichTextEditor.dispatch(editor, 'insertHtml', { html });
            RichTextEditor.afterChange(editor);
        }

        public static tableOp(editor: any, op: string) {
            if (!editor || editor._readOnly) return;
            // Restore the editor selection so the operation targets the cell the user last
            // selected in the editor, not a selection left in the toolbar.
            RichTextEditor.restoreSelection(editor);
            const cell = RichTextEditor.cellAtSelection(editor) as HTMLTableCellElement | null;
            if (!cell) return;
            const row = cell.parentElement as HTMLTableRowElement;
            const table = cell.closest('table') as HTMLTableElement | null;
            if (!table || !row) return;

            switch (op) {
                case 'addRow': RichTextEditor.insertTableRow(table, row, false); break;
                case 'addRowBefore': RichTextEditor.insertTableRow(table, row, true); break;
                case 'addCol': RichTextEditor.insertTableColumn(table, cell, false); break;
                case 'addColBefore': RichTextEditor.insertTableColumn(table, cell, true); break;
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
                case 'split': {
                    RichTextEditor.splitCell(table, cell);
                    break;
                }
                case 'headerRow': {
                    RichTextEditor.toggleHeaderRow(table);
                    break;
                }
                case 'delTable': {
                    table.remove();
                    break;
                }
            }
            RichTextEditor.afterChange(editor);
        }

        // Inserts a full-width row next to the given one. Cells whose rowspan straddles the
        // insertion boundary are stretched instead of split, so a merged region keeps covering the
        // rows it already spanned.
        private static insertTableRow(table: HTMLTableElement, row: HTMLTableRowElement, before: boolean) {
            const { rows, grid, colCount } = RichTextEditor.buildTableGrid(table);
            const ri = rows.indexOf(row);
            if (ri < 0) return;
            // The neighbour on the far side of the boundary: a cell present in both the current row
            // and that neighbour is one that spans across the boundary.
            const neighbourIndex = before ? ri - 1 : ri + 1;
            const neighbour = (neighbourIndex >= 0 && neighbourIndex < rows.length && grid[neighbourIndex])
                ? grid[neighbourIndex]
                : null;
            const nr = document.createElement('tr');
            const extended = new Set<HTMLTableCellElement>();
            for (let c = 0; c < colCount; c++) {
                const here = grid[ri] ? (grid[ri][c] || null) : null;
                const across = neighbour ? (neighbour[c] || null) : null;
                if (here && here === across) {
                    if (!extended.has(here)) { extended.add(here); here.rowSpan = (here.rowSpan || 1) + 1; }
                    continue;
                }
                const td = document.createElement(here && here.tagName === 'TH' ? 'th' : 'td');
                td.innerHTML = '<br>';
                nr.appendChild(td);
            }
            if (nr.childElementCount === 0) return;
            if (before) row.before(nr); else row.after(nr);
        }

        // Inserts a column next to the one holding the given cell. A cell whose colspan straddles
        // the insertion boundary is widened once instead of receiving a new neighbour.
        private static insertTableColumn(table: HTMLTableElement, cell: HTMLTableCellElement, before: boolean) {
            const { rows, grid } = RichTextEditor.buildTableGrid(table);
            const targetCol = RichTextEditor.logicalColumnOf(grid, cell);
            if (targetCol < 0) return;
            const insertCol = before ? targetCol : targetCol + 1;
            const widened = new Set<HTMLTableCellElement>();
            for (let r = 0; r < rows.length; r++) {
                const left = insertCol > 0 ? (grid[r][insertCol - 1] || null) : null;
                const at = grid[r][insertCol] || null;
                if (left && left === at) {
                    if (!widened.has(left)) { widened.add(left); left.colSpan = (left.colSpan || 1) + 1; }
                    continue;
                }
                const reference = at || left;
                const td = document.createElement(reference && reference.tagName === 'TH' ? 'th' : 'td');
                td.innerHTML = '<br>';
                if (at && at.parentElement === rows[r]) at.before(td);
                else if (left && left.parentElement === rows[r]) left.after(td);
                else rows[r].appendChild(td);
            }
        }

        // Undoes a merge: every logical position the cell covers gets its own empty cell back and
        // the origin cell keeps the content.
        private static splitCell(table: HTMLTableElement, cell: HTMLTableCellElement) {
            const colspan = Math.max(1, cell.colSpan || 1);
            const rowspan = Math.max(1, cell.rowSpan || 1);
            if (colspan === 1 && rowspan === 1) return;

            const { rows, grid } = RichTextEditor.buildTableGrid(table);
            const startRow = rows.indexOf(cell.parentElement as HTMLTableRowElement);
            const startCol = RichTextEditor.logicalColumnOf(grid, cell);
            if (startRow < 0 || startCol < 0) return;

            cell.removeAttribute('colspan');
            cell.removeAttribute('rowspan');

            for (let r = startRow; r < startRow + rowspan && r < rows.length; r++) {
                // Walk right to left so each insertion leaves the columns still to be filled valid.
                for (let c = startCol + colspan - 1; c >= startCol; c--) {
                    if (r === startRow && c === startCol) continue;
                    const td = document.createElement(cell.tagName === 'TH' ? 'th' : 'td');
                    td.innerHTML = '<br>';
                    // Anchor on the first cell of this row that starts after the released region.
                    let ref: HTMLTableCellElement | null = null;
                    for (let k = c + 1; k < grid[r].length; k++) {
                        const cand = grid[r][k];
                        if (cand && cand !== cell && cand.parentElement === rows[r]) { ref = cand; break; }
                    }
                    if (ref) rows[r].insertBefore(td, ref);
                    else if (r === startRow) cell.after(td);
                    else rows[r].appendChild(td);
                }
            }
        }

        // Promotes the first row to a thead of th cells, or demotes it back to body cells.
        private static toggleHeaderRow(table: HTMLTableElement) {
            const rows = (Array.from(table.querySelectorAll('tr')) as HTMLTableRowElement[])
                .filter(tr => tr.closest('table') === table);
            const first = rows[0];
            if (!first || first.childElementCount === 0) return;

            const isHeader = first.parentElement?.tagName === 'THEAD'
                || Array.from(first.children).every(c => c.tagName === 'TH');

            const retag = (from: HTMLTableRowElement, tag: string) => {
                Array.from(from.children).forEach(child => {
                    if (child.tagName === tag.toUpperCase()) return;
                    const next = document.createElement(tag);
                    Array.from(child.attributes).forEach(a => next.setAttribute(a.name, a.value));
                    while (child.firstChild) next.appendChild(child.firstChild);
                    child.replaceWith(next);
                });
            };

            if (isHeader) {
                retag(first, 'td');
                const thead = first.parentElement;
                if (thead && thead.tagName === 'THEAD') {
                    let tbody = table.querySelector(':scope > tbody');
                    if (!tbody) { tbody = document.createElement('tbody'); thead.after(tbody); }
                    tbody.insertBefore(first, tbody.firstChild);
                    if (thead.childElementCount === 0) thead.remove();
                }
                return;
            }

            retag(first, 'th');
            if (first.parentElement?.tagName !== 'THEAD') {
                const thead = document.createElement('thead');
                table.insertBefore(thead, table.firstChild);
                thead.appendChild(first);
            }
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

        public static find(editor: any, term: string, caseSensitive: boolean, wholeWord?: boolean): number {
            RichTextEditor.clearFind(editor);
            if (!term) return 0;
            const rx = RichTextEditor.buildFindRegex(term, caseSensitive, wholeWord === true);
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
            RichTextEditor.highlightCurrentMatch(editor);
            return count;
        }

        // Builds the match expression shared by find/replaceAll. Whole-word mode anchors the term
        // between non-word characters, which is what every editor's "match whole word" toggle means.
        private static buildFindRegex(term: string, caseSensitive: boolean, wholeWord: boolean): RegExp {
            const flags = caseSensitive ? 'g' : 'gi';
            const body = RichTextEditor.escapeRegExp(term);
            return wholeWord
                ? new RegExp(`(?<![\\p{L}\\p{N}_])${body}(?![\\p{L}\\p{N}_])`, flags + 'u')
                : new RegExp(body, flags);
        }

        // Moves the current-match cursor by delta (wrapping) and returns its 1-based position, or 0
        // when there is nothing to step through. Keeps the active match highlighted and in view.
        public static findStep(editor: any, delta: number): number {
            if (!editor) return 0;
            const marks = editor.querySelectorAll('mark.bit-rte-find');
            if (marks.length === 0) { editor._findIndex = -1; return 0; }
            const current = typeof editor._findIndex === 'number' && editor._findIndex >= 0 ? editor._findIndex : 0;
            const next = ((current + delta) % marks.length + marks.length) % marks.length;
            editor._findIndex = next;
            RichTextEditor.highlightCurrentMatch(editor);
            return next + 1;
        }

        // Marks the active match so it reads differently from the rest and scrolls it into view.
        private static highlightCurrentMatch(editor: any) {
            const marks = editor.querySelectorAll('mark.bit-rte-find') as NodeListOf<HTMLElement>;
            const idx = editor._findIndex;
            marks.forEach((m, i) => m.classList.toggle('bit-rte-find-cur', i === idx));
            if (idx >= 0 && idx < marks.length) {
                marks[idx].scrollIntoView({ block: 'nearest', inline: 'nearest' });
            }
        }

        public static replaceCurrent(editor: any, term: string, replacement: string, caseSensitive: boolean, wholeWord?: boolean): number {
            if (!editor || editor._readOnly) return 0;
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
            const remaining = RichTextEditor.find(editor, term, caseSensitive, wholeWord);
            // Keep the cursor on the match that took the replaced one's place rather than snapping
            // back to the first hit, so repeated Replace walks forward through the document.
            if (remaining > 0) {
                editor._findIndex = Math.min(idx, remaining - 1);
                RichTextEditor.highlightCurrentMatch(editor);
            }
            return remaining;
        }

        public static replaceAll(editor: any, term: string, replacement: string, caseSensitive: boolean, wholeWord?: boolean): number {
            if (!editor || editor._readOnly) return 0;
            RichTextEditor.clearFind(editor);
            if (!term) return 0;
            const rx = RichTextEditor.buildFindRegex(term, caseSensitive, wholeWord === true);
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
            if (!editor || editor._readOnly) return;
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
                else if (e.key === 'Escape') {
                    // Escape hands focus back to the text, so the toolbar is never a dead end for
                    // a keyboard user who entered it with Alt+F10 or a Tab.
                    e.preventDefault();
                    const surface = toolbar.closest('.bit-rte')?.querySelector('.bit-rte-edt') as HTMLElement | null;
                    surface?.focus();
                    return;
                }
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
            // Single choke point for every mutating operation: a read-only (or disabled) editor
            // never runs a command, whichever path asked for it.
            if (editor._readOnly) return false;
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
                case 'insertTaskList':
                    return RichTextEditor.toggleTaskList(editor, false);
                case 'inlineCode':
                    return RichTextEditor.toggleInlineCode(editor);
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
            if (editor._mdBusy || editor._readOnly) return;
            const block = RichTextEditor.currentBlock(editor);
            if (!block) return;
            const text = block.textContent || '';

            if (e.inputType === 'insertText' && e.data === '/' && text === '/') {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnSlashTrigger');
                return;
            }

            // A mention starts at a word boundary, so "@" only triggers at the start of the block or
            // after whitespace - never inside an email address or a handle already being typed.
            if (editor._mentions && e.inputType === 'insertText' && e.data === '@'
                && RichTextEditor.atMentionBoundary(editor)) {
                if (editor._dotNetRef) editor._dotNetRef.invokeMethodAsync('OnMentionTrigger');
                return;
            }

            if (e.inputType !== 'insertText' || e.data !== ' ') return;

            // A trailing URL is linked as soon as the word is finished, matching the autolink
            // behavior every mainstream editor ships. Done before the block markers so a line that
            // merely ends in a URL is not mistaken for a marker.
            if (RichTextEditor.autoLinkAtCaret(editor)) return;

            const map: { [key: string]: string } = {
                '#': 'h1', '##': 'h2', '###': 'h3',
                '####': 'h4', '#####': 'h5', '######': 'h6',
                '>': 'blockquote',
                '```': 'pre', '~~~': 'pre'
            };
            const marker = text.trim();
            const run = (fn: () => void) => {
                editor._mdBusy = true;
                RichTextEditor.clearBlockText(block);
                fn();
                editor._mdBusy = false;
                RichTextEditor.afterChange(editor);
            };

            if (map[marker]) {
                run(() => RichTextEditor.dispatch(editor, 'formatBlock', { value: map[marker] }));
            } else if (marker === '-' || marker === '*' || marker === '+') {
                run(() => RichTextEditor.dispatch(editor, 'insertUnorderedList', {}));
            } else if (marker === '1.' || marker === '1)') {
                run(() => RichTextEditor.dispatch(editor, 'insertOrderedList', {}));
            } else if (marker === '[]' || marker === '[ ]' || marker === '[x]' || marker === '[X]') {
                const checked = marker === '[x]' || marker === '[X]';
                run(() => RichTextEditor.toggleTaskList(editor, checked));
            } else if (marker === '---' || marker === '***' || marker === '___') {
                run(() => RichTextEditor.insertHorizontalRule(editor));
            }
        }

        // Wraps the bare URL immediately before the caret in an anchor. Returns true when a link
        // was created so the caller can skip the block-marker rules for that keystroke.
        private static autoLinkAtCaret(editor: any): boolean {
            if (editor._autoLink === false) return false;
            if (!RichTextEditor.isTagAllowed(editor, 'a') || !RichTextEditor.isAttrAllowed(editor, 'a', 'href')) return false;
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0 || !sel.isCollapsed) return false;
            const node = sel.anchorNode;
            if (!node || node.nodeType !== 3 || !editor.contains(node)) return false;
            // Never nest a link inside an existing one.
            if (RichTextEditor.linkAtSelection(editor)) return false;

            const text = node.nodeValue || '';
            // The caret sits just after the space that finished the word, so the candidate is the
            // last whitespace-delimited token before it.
            const upto = text.slice(0, Math.max(0, sel.anchorOffset - 1));
            const match = /(^|\s)((?:https?:\/\/|www\.)[^\s<>"']{2,})$/.exec(upto);
            if (!match) return false;
            const candidate = match[2];
            // Trailing sentence punctuation belongs to the sentence, not to the URL.
            const raw = candidate.replace(/[.,;:!?)\]]+$/, '');
            if (!raw) return false;
            const href = /^www\./i.test(raw) ? `https://${raw}` : raw;
            if (!RichTextEditor.isAllowedUri(editor, href, false)) return false;

            // The match ends at the end of `upto`, so the URL starts a full candidate-length back -
            // measuring from the trimmed length would slide the range past the start of the URL.
            const start = upto.length - candidate.length;
            const range = document.createRange();
            range.setStart(node, start);
            range.setEnd(node, start + raw.length);
            const anchor = document.createElement('a');
            anchor.setAttribute('href', href);
            try { range.surroundContents(anchor); } catch { return false; }
            // Put the caret back after the trailing space so typing continues outside the link.
            const after = document.createRange();
            after.setStartAfter(anchor);
            after.collapse(true);
            sel.removeAllRanges();
            sel.addRange(after);
            editor._range = after.cloneRange();
            RichTextEditor.afterChange(editor);
            return true;
        }

        // Turns the current block into a checklist item (or back into a plain list item when it
        // already is one). A task list is a plain <ul class="bit-rte-tasks"> whose items carry
        // data-checked, so it survives the sanitizer allowlist and needs no interactive element.
        private static toggleTaskList(editor: any, checked: boolean): boolean {
            const li = RichTextEditor.listItemAtSelection(editor);
            const list = li ? (li.closest('ul') as HTMLElement | null) : null;
            if (list && list.classList.contains('bit-rte-tasks')) {
                list.classList.remove('bit-rte-tasks');
                list.querySelectorAll('li').forEach((i: Element) => i.removeAttribute('data-checked'));
                return true;
            }
            if (!list) {
                if (!RichTextEditor.dispatch(editor, 'insertUnorderedList', {})) return false;
            }
            const item = RichTextEditor.listItemAtSelection(editor);
            const ul = item ? (item.closest('ul') as HTMLElement | null) : null;
            if (!ul) return false;
            ul.classList.add('bit-rte-tasks');
            ul.querySelectorAll('li').forEach((i: Element) => {
                if (!i.hasAttribute('data-checked')) i.setAttribute('data-checked', 'false');
            });
            if (item && checked) item.setAttribute('data-checked', 'true');
            return true;
        }

        // Clicking a checklist item's marker toggles it. The marker is drawn by CSS at the start of
        // the item, so a click in that gutter (before the item's content box) is the toggle.
        private static enableTaskToggle(editor: any) {
            if (!editor || editor._taskWired) return;
            editor._taskWired = true;
            editor.addEventListener('click', (e: MouseEvent) => {
                if (editor._readOnly) return;
                const target = e.target as HTMLElement;
                const li = target && target.closest ? (target.closest('li') as HTMLElement | null) : null;
                if (!li || !li.parentElement || !li.parentElement.classList.contains('bit-rte-tasks')) return;
                const rect = li.getBoundingClientRect();
                const rtl = getComputedStyle(li).direction === 'rtl';
                const inMarker = rtl ? e.clientX > rect.right - 24 : e.clientX < rect.left + 24;
                if (!inMarker) return;
                e.preventDefault();
                li.setAttribute('data-checked', li.getAttribute('data-checked') === 'true' ? 'false' : 'true');
                RichTextEditor.afterChange(editor);
            });
        }

        // Whether the "@" just typed begins a word: the caret sits right after it, and what comes
        // before it is either nothing or whitespace.
        private static atMentionBoundary(editor: any): boolean {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0 || !sel.isCollapsed) return false;
            const node = sel.anchorNode;
            if (!node || node.nodeType !== 3 || !editor.contains(node)) return false;
            const offset = sel.anchorOffset;
            const text = node.nodeValue || '';
            if (offset < 1 || text[offset - 1] !== '@') return false;
            if (offset === 1) return true;
            return /\s/.test(text[offset - 2]);
        }

        // Replaces the trigger character with the picked mention's markup. The trigger is removed
        // first so the inserted mention reads as one token rather than "@@name".
        public static applyMention(editor: any, html: string) {
            if (!editor || !html || editor._readOnly) return;
            RichTextEditor.restoreSelection(editor);
            if (!RichTextEditor.fitsWithinMaxLength(editor, html)) {
                RichTextEditor.reportClientError(editor, 'max-length', 'The content would exceed the maximum length.');
                return;
            }

            const sel = document.getSelection();
            if (sel && sel.rangeCount > 0 && sel.isCollapsed) {
                const current = sel.getRangeAt(0);
                const node = current.startContainer;
                if (node.nodeType === 3 && current.startOffset > 0
                    && (node.nodeValue || '')[current.startOffset - 1] === '@') {
                    const trigger = document.createRange();
                    trigger.setStart(node, current.startOffset - 1);
                    trigger.setEnd(node, current.startOffset);
                    trigger.deleteContents();
                    trigger.collapse(true);
                    sel.removeAllRanges();
                    sel.addRange(trigger);
                    editor._range = trigger.cloneRange();
                }
            }

            RichTextEditor.dispatch(editor, 'insertHtml', { html: RichTextEditor.sanitize(editor, html) });
            RichTextEditor.afterChange(editor);
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
                if (editor._readOnly) return;
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
                if (editor._readOnly) { RichTextEditor.removeResizeHandle(editor); return; }
                const target = e.target as HTMLElement;
                if (target && target.tagName === 'IMG') {
                    editor._activeImage = target as HTMLImageElement;
                    RichTextEditor.startImageResize(editor, target as HTMLImageElement);
                } else {
                    editor._activeImage = null;
                    RichTextEditor.removeResizeHandle(editor);
                }
                RichTextEditor.reportState(editor);
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
            if (!editor || editor._readOnly) return;
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
            if (editor._readOnly) { e.preventDefault(); return; }
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
            if (editor._readOnly) { e.preventDefault(); return; }
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
            // Alt+F10 is the conventional "move focus to the toolbar" gesture for an editing
            // surface, and it is the only way to reach the toolbar from the keyboard without
            // shift-tabbing back past the whole editor. It works even while read-only, since the
            // toolbar is still reachable (source view, full screen).
            if (e.altKey && e.key === 'F10') {
                e.preventDefault();
                RichTextEditor.focusToolbar(editor);
                return;
            }

            if (editor._readOnly) return;

            // Tab indents / Shift+Tab outdents while the caret is inside a list, matching every
            // other editor. Outside a list the key keeps its native meaning (move focus out of the
            // editor), so the editor never becomes a keyboard trap.
            if (e.key === 'Tab' && !e.ctrlKey && !e.metaKey && !e.altKey) {
                if (RichTextEditor.listItemAtSelection(editor)) {
                    e.preventDefault();
                    RichTextEditor.dispatch(editor, e.shiftKey ? 'outdent' : 'indent', {});
                    RichTextEditor.afterChange(editor);
                }
                return;
            }

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
            // A read-only surface is not contenteditable, but an assistive tool or an extension can
            // still raise beforeinput against it; refuse the edit rather than relying on that.
            if (editor._readOnly) { e.preventDefault(); return; }
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
            const rect = editor._quickToolbar ? RichTextEditor.selectionRect(editor) : null;
            const image = RichTextEditor.selectedImage(editor);
            const codeEl = RichTextEditor.ancestorTag(editor, 'CODE');
            const inlineCode = codeEl && !(codeEl.parentElement && codeEl.parentElement.tagName === 'PRE');
            const listItem = RichTextEditor.listItemAtSelection(editor);
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
                justifyFull: q('justifyFull'),
                block: block,
                subscript: q('subscript'),
                superscript: q('superscript'),
                foreColor: RichTextEditor.toHexColor(v('foreColor')),
                backColor: RichTextEditor.toHexColor(v('backColor')),
                fontName: (v('fontName') || '').replace(/^['"]|['"]$/g, '') || null,
                // queryCommandValue('fontSize') reports the legacy 1-7 scale, which never matches
                // the CSS lengths the font-size selector offers. Read the resolved size instead so
                // the selector reflects what is actually applied.
                fontSize: RichTextEditor.computedFontSize(editor),
                direction: RichTextEditor.directionAtSelection(editor),
                inLink: !!link,
                linkHref: link ? link.getAttribute('href') : null,
                inlineCode: !!inlineCode,
                taskList: !!(listItem && listItem.parentElement && listItem.parentElement.classList.contains('bit-rte-tasks')),
                inTable: !!RichTextEditor.cellAtSelection(editor),
                hasSelection: !!rect,
                selectionTop: rect ? rect.top : 0,
                selectionLeft: rect ? rect.left : 0,
                selectionWidth: rect ? rect.width : 0,
                selectionHeight: rect ? rect.height : 0,
                imageSelected: !!image,
                imageAlign: image ? RichTextEditor.imageAlignOf(image) : null
            };
        }

        // The bounding box of the current (non-collapsed) selection, in coordinates relative to the
        // component root - which is the positioning context the selection toolbar is placed in.
        // Returns null whenever there is nothing to anchor to: a collapsed caret, a selection that
        // reaches outside this editor, or a zero-sized range.
        private static selectionRect(editor: any): { top: number, left: number, width: number, height: number } | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return null;
            const range = sel.getRangeAt(0);
            if (!editor.contains(range.startContainer) || !editor.contains(range.endContainer)) return null;
            const root = editor.closest('.bit-rte');
            if (!root) return null;
            const rect = range.getBoundingClientRect();
            if (!rect || (rect.width === 0 && rect.height === 0)) return null;
            const origin = root.getBoundingClientRect();
            return {
                top: rect.top - origin.top,
                left: rect.left - origin.left,
                width: rect.width,
                height: rect.height
            };
        }

        // The image the user last clicked, as long as it is still in this editor. Clicking anywhere
        // else clears it (see enableImageResize), so this doubles as "an image is selected".
        private static selectedImage(editor: any): HTMLImageElement | null {
            const img = editor._activeImage as HTMLImageElement | null;
            if (!img || !editor.contains(img)) return null;
            return img;
        }

        // Which of the three alignments an image currently carries, or null when it flows inline.
        private static imageAlignOf(img: HTMLImageElement): string | null {
            const float = (img.style.float || '').toLowerCase();
            if (float === 'left' || float === 'right') return float;
            if ((img.style.display || '').toLowerCase() === 'block'
                && (img.style.marginLeft || '').toLowerCase() === 'auto') return 'center';
            return null;
        }

        // Floats the selected image left or right, centers it as its own block, or clears the
        // alignment. The alignment is written as inline style because that is what survives the
        // sanitizer's CSS allowlist and what a consumer of the HTML will render.
        public static alignImage(editor: any, align: string) {
            if (!editor || editor._readOnly) return;
            const img = RichTextEditor.selectedImage(editor);
            if (!img) return;

            img.style.removeProperty('float');
            img.style.removeProperty('display');
            img.style.removeProperty('margin-left');
            img.style.removeProperty('margin-right');

            if (align === 'left' || align === 'right') {
                img.style.float = align;
                img.style[align === 'left' ? 'marginRight' : 'marginLeft'] = '1em';
            } else if (align === 'center') {
                img.style.display = 'block';
                img.style.marginLeft = 'auto';
                img.style.marginRight = 'auto';
            }
            RichTextEditor.afterChange(editor);
        }

        // Normalizes a queryCommandValue color ("rgb(1, 2, 3)", "#abc", a color keyword) into the
        // "#rrggbb" form an <input type="color"> can display. Returns null when the value is
        // missing or cannot be resolved, which the toolbar reads as "no single active color".
        private static toHexColor(value: string): string | null {
            const raw = (value || '').trim();
            if (!raw || raw === 'transparent') return null;

            const rgb = /^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i.exec(raw);
            if (rgb) {
                const hex = (n: string) => Math.max(0, Math.min(255, parseInt(n, 10) || 0)).toString(16).padStart(2, '0');
                return `#${hex(rgb[1])}${hex(rgb[2])}${hex(rgb[3])}`;
            }
            if (/^#[0-9a-f]{6}$/i.test(raw)) return raw.toLowerCase();
            if (/^#[0-9a-f]{3}$/i.test(raw)) {
                return `#${raw[1]}${raw[1]}${raw[2]}${raw[2]}${raw[3]}${raw[3]}`.toLowerCase();
            }
            return null;
        }

        // The resolved font-size at the selection, as a CSS length, or null when the selection is
        // outside this editor.
        private static computedFontSize(editor: any): string | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            if (node && node.nodeType === 3) node = node.parentNode;
            if (!node || !editor.contains(node)) return null;
            const size = getComputedStyle(node as Element).fontSize;
            return size || null;
        }

        private static computeFacts(editor: any): any {
            const text = (editor.textContent || '').replace(/\u00a0/g, ' ');
            const hasText = text.trim().length > 0;
            const hasEmbedded = !!editor.querySelector('img,table,hr,audio,video,iframe');
            const words = (text.trim().match(/\S+/g) || []).length;
            return {
                hasText: hasText,
                hasEmbeddedContent: hasEmbedded,
                // Whitespace-only content reads as empty, so it counts as zero characters rather
                // than as the length of the padding the browser left behind. Otherwise the count
                // is the visible text length, which is exactly what MaxLength budgets against.
                characterCount: hasText ? text.length : 0,
                wordCount: words
            };
        }

        // ====================================================================
        // Helpers
        // ====================================================================
        // Moves keyboard focus to the first enabled control of this editor's toolbar (Alt+F10).
        private static focusToolbar(editor: any) {
            const toolbar = editor?.closest('.bit-rte')?.querySelector('.bit-rte-tlb') as HTMLElement | null;
            if (!toolbar) return;
            const first = (Array.from(toolbar.querySelectorAll('button,select,input')) as HTMLElement[])
                .find(el => !(el as HTMLButtonElement).disabled);
            first?.focus();
        }

        // The <li> containing the selection, or null when the caret is not inside a list.
        private static listItemAtSelection(editor: any): HTMLElement | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            if (node && !editor.contains(node)) return null;
            while (node && node !== editor) {
                if (node.nodeType === 1 && node.tagName === 'LI') return node;
                node = node.parentNode;
            }
            return null;
        }

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

        // Toggles <code> around the selection. execCommand has no inline-code command, so the
        // wrap/unwrap is done over the range directly; the surrounding markup is preserved because
        // the existing nodes are moved into (or out of) the <code> element rather than reserialized.
        private static toggleInlineCode(editor: any): boolean {
            if (!RichTextEditor.isTagAllowed(editor, 'code')) return false;
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return false;

            const existing = RichTextEditor.ancestorTag(editor, 'CODE');
            if (existing) {
                // Only unwrap an inline <code>; a <code> inside a <pre> is the code block itself.
                if (existing.parentElement && existing.parentElement.tagName === 'PRE') return false;
                const parent = existing.parentNode;
                existing.replaceWith(...Array.from(existing.childNodes));
                parent && (parent as Element).normalize?.();
                return true;
            }

            const range = sel.getRangeAt(0);
            if (range.collapsed) return false;
            if (!editor.contains(range.startContainer) || !editor.contains(range.endContainer)) return false;
            const code = document.createElement('code');
            try {
                code.appendChild(range.extractContents());
                range.insertNode(code);
            } catch {
                return false;
            }
            const after = document.createRange();
            after.selectNodeContents(code);
            sel.removeAllRanges();
            sel.addRange(after);
            editor._range = after.cloneRange();
            return true;
        }

        // Nearest ancestor of the selection with the given tag name, bounded by the editor.
        private static ancestorTag(editor: any, tagName: string): HTMLElement | null {
            const sel = document.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            let node: any = sel.anchorNode;
            if (node && !editor.contains(node)) return null;
            while (node && node !== editor) {
                if (node.nodeType === 1 && node.tagName === tagName) return node;
                node = node.parentNode;
            }
            return null;
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
            // Sanitize repeatedly until the serialized output stops changing. Parsing markup and
            // re-serializing it can produce a *different* tree the second time around (mutation
            // XSS via namespace switching, badly closed comments, ...), so a single pass can hand
            // back a string that becomes dangerous when the browser parses it again. Re-running
            // the pass on our own output closes that gap; three rounds is far past the point any
            // known vector stabilizes and bounds the cost for pathological input.
            let current = html ?? '';
            for (let pass = 0; pass < 3; pass++) {
                const next = RichTextEditor.sanitizePass(editor, current);
                if (next === current) return next;
                current = next;
            }
            return current;
        }

        private static sanitizePass(editor: any, html: string): string {
            const tpl = document.createElement('template');
            tpl.innerHTML = html;
            const policy = (editor && editor._policy) || RichTextEditor.DEFAULT_POLICY;

            // Comments are never rendered content and are a known re-parse vector ("<!--><script>"
            // style payloads), so drop them before anything else looks at the tree.
            const comments = document.createTreeWalker(tpl.content, NodeFilter.SHOW_COMMENT);
            const staleComments: Node[] = [];
            while (comments.nextNode()) staleComments.push(comments.currentNode);
            staleComments.forEach(c => c.parentNode && c.parentNode.removeChild(c));

            tpl.content.querySelectorAll(RichTextEditor.DROPPED_TAGS).forEach((n: Element) => {
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
                    if (!allowed.includes(name)) { el.removeAttribute(attr.name); continue; }
                    // A permitted 'style' attribute still has its declarations filtered: only the
                    // presentational properties the editor itself emits survive, and any value that
                    // could reference a URL or a script is dropped.
                    if (name === 'style') {
                        const safeStyle = RichTextEditor.sanitizeStyle(val);
                        if (safeStyle) el.setAttribute('style', safeStyle);
                        else el.removeAttribute(attr.name);
                    }
                }
                if (tag === 'iframe' && !(editor && editor._policy)) {
                    if (!RichTextEditor.isAllowedEmbedSrc(el.getAttribute('src'))) {
                        el.remove();
                        return;
                    }
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

        // Filters a style attribute down to the allowlisted presentational declarations. Returns
        // the rebuilt declaration list, or an empty string when nothing survives (the caller then
        // removes the attribute entirely rather than leaving an empty one behind).
        private static sanitizeStyle(style: string): string {
            const kept: string[] = [];
            for (const decl of (style || '').split(';')) {
                const colon = decl.indexOf(':');
                if (colon <= 0) continue;
                const prop = decl.slice(0, colon).trim().toLowerCase();
                const value = decl.slice(colon + 1).trim();
                if (!value) continue;
                if (!RichTextEditor.ALLOWED_CSS_PROPS.has(prop)) continue;
                if (RichTextEditor.UNSAFE_CSS_VALUE.test(value)) continue;
                kept.push(`${prop}: ${value}`);
            }
            return kept.join('; ');
        }

        // Cleans up the wrappers Word and Google Docs put around copied content before the
        // markup reaches the allowlist. Without this the pasted text arrives buried in namespaced
        // elements and mso-* declarations that would either survive as noise or take the real
        // formatting with them when they are stripped.
        private static normalizeWordHtml(html: string): string {
            return html
                .replace(/<!--[\s\S]*?-->/g, '')
                // Word ships an <xml> island of document metadata alongside the content.
                .replace(/<xml[\s\S]*?<\/xml>/gi, '')
                .replace(/<\/?o:[^>]*>/gi, '')
                .replace(/<\/?w:[^>]*>/gi, '')
                .replace(/<\/?m:[^>]*>/gi, '')
                .replace(/<\/?st\d+:[^>]*>/gi, '')
                .replace(/\s(class|style)="[^"]*mso[^"]*"/gi, '')
                // Google Docs wraps everything in <b style="font-weight:normal" id="docs-internal-guid-...">,
                // which would otherwise bold the whole paste once the style attribute is filtered.
                .replace(/<b[^>]*id="docs-internal-guid-[^"]*"[^>]*>/gi, '');
        }

        private static escapeHtml(s: string): string {
            const d = document.createElement('div');
            d.textContent = s ?? '';
            return d.innerHTML;
        }

        // Whether inserting a fragment at the current selection keeps the visible text within
        // _maxLength. Selected text is replaced by the insert, so it frees its own length.
        private static fitsWithinMaxLength(editor: any, html: string): boolean {
            const max = editor._maxLength;
            if (max == null) return true;
            const sel = document.getSelection();
            const selected = (sel && !sel.isCollapsed) ? sel.toString().length : 0;
            const current = (editor.textContent || '').length;
            return current - selected + RichTextEditor.visibleTextLength(html) <= max;
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

        // Whether an iframe source is one of the approved embed hosts, over https. Anything else -
        // including an unparseable value - is refused, so a downgraded or unknown embed is dropped
        // rather than rendered.
        private static isAllowedEmbedSrc(src: string | null): boolean {
            const value = (src || '').trim();
            if (!value) return false;
            try {
                const url = new URL(value);
                return url.protocol.toLowerCase() === 'https:'
                    && RichTextEditor.IFRAME_HOSTS.includes(url.host.toLowerCase());
            } catch {
                return false;
            }
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
