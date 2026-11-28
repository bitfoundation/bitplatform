namespace BitBlazorUI {
    // bit-trim: on-demand
    // The inline marks of BitRichTextEditor (BitRichTextEditor.ts): bold, italic, underline, strikethrough, subscript,
    // superscript and inline code, and clearing them. They used to be document.execCommand's, which writes other markup
    // in every engine for anything past a plain selection; here they are DOM edits of the editor's own, so the same
    // command leaves the same markup in Blink, Gecko and WebKit. Everything works on a root element and a Range and
    // keeps no state: the editor script owns the selection, the history and the marks pending at a caret.
    //
    // A mark is read in every spelling it has (b and strong are both bold), and written in the one the toolbar has
    // always written (b, i, u, strike, sub, sup, code), so content saved by an earlier version keeps its markup. The
    // colors and fonts are styles split and joined the same way, each on a span (<span style="color: red">), and a
    // link is one anchor per block around what it links.

    export type BitRichTextEditorMark =
        'bold' | 'italic' | 'underline' | 'strikeThrough' | 'subscript' | 'superscript' | 'inlineCode';

    export class BitRichTextEditorMarks {
        // The tag each mark writes.
        public static readonly WRITES: { [mark: string]: string } = {
            bold: 'b', italic: 'i', underline: 'u', strikeThrough: 'strike', subscript: 'sub', superscript: 'sup', inlineCode: 'code',
        };

        // Every tag that already counts as the mark.
        public static readonly READS: { [mark: string]: string[] } = {
            bold: ['B', 'STRONG'], italic: ['I', 'EM'], underline: ['U'], strikeThrough: ['S', 'STRIKE', 'DEL'],
            subscript: ['SUB'], superscript: ['SUP'], inlineCode: ['CODE'],
        };

        // A mark that cannot hold together with another: text is either lowered or raised.
        public static readonly EXCLUDES: { [mark: string]: string } = { subscript: 'superscript', superscript: 'subscript' };

        // What clearing the formatting unwraps: the inline formatting elements, and a span with nothing but a style.
        // Links stay, and so does a span that carries more than a style (a mention) and a find highlight.
        private static readonly FORMATTING = /^(B|STRONG|I|EM|U|S|STRIKE|DEL|INS|SUB|SUP|CODE|FONT|BIG|SMALL|TT|KBD|SAMP|VAR|CITE|DFN|Q|MARK|SPAN)$/;

        private static readonly BLOCK = /^(ADDRESS|ARTICLE|ASIDE|BLOCKQUOTE|DD|DETAILS|DIV|DL|DT|FIGCAPTION|FIGURE|FOOTER|H[1-6]|HEADER|HR|LI|MAIN|NAV|OL|P|PRE|SECTION|SUMMARY|TABLE|TBODY|TD|TFOOT|TH|THEAD|TR|UL)$/;

        // Parents whose whitespace text is the source's indentation between blocks, never content.
        private static readonly STRUCTURAL = /^(UL|OL|DL|TABLE|TBODY|THEAD|TFOOT|TR|COLGROUP)$/;

        private static readonly VOID = 'img,br,hr,iframe,video,audio,input,svg';

        public static isMark(name: string): name is BitRichTextEditorMark {
            return Object.prototype.hasOwnProperty.call(BitRichTextEditorMarks.WRITES, name);
        }

        // The nearest element between node and root that is the mark, or null. Inline code is never the code of a code
        // block (a <code> in a <pre>), and nothing inside a code block is inline code.
        public static markOf(root: Node, node: Node, mark: BitRichTextEditorMark): HTMLElement | null {
            const tags = BitRichTextEditorMarks.READS[mark];
            for (let n: Node | null = node.nodeType === 1 ? node : node.parentNode; n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                if (mark === 'inlineCode' && n.nodeName === 'PRE') return null;
                if (tags.includes(n.nodeName)) {
                    if (mark === 'inlineCode' && n.parentNode && n.parentNode.nodeName === 'PRE') return null;
                    return n as HTMLElement;
                }
            }
            return null;
        }

        public static inCodeBlock(root: Node, node: Node): boolean {
            for (let n: Node | null = node; n && n !== root; n = n.parentNode) {
                if (n.nodeName === 'PRE') return true;
            }
            return false;
        }

        // Whether the mark is on: at a caret, on the text it sits in; over a selection, on every text the selection holds
        // (its whitespace aside, unless it holds nothing else).
        public static isActive(root: Node, range: Range, mark: BitRichTextEditorMark): boolean {
            if (range.collapsed) {
                const at = BitRichTextEditorMarks.caretNode(range);
                return !!at && root.contains(at) && !!BitRichTextEditorMarks.markOf(root, at, mark);
            }
            const texts = BitRichTextEditorMarks.counted(BitRichTextEditorMarks.textsIn(root, range).map(t => t.node));
            return texts.length > 0 && texts.every(t => !!BitRichTextEditorMarks.markOf(root, t, mark));
        }

        // Adds the mark to the selection, or takes it off where every text of it already has it. Returns the range
        // the selection should then cover, or null when nothing changed.
        public static toggle(root: Node, range: Range, mark: BitRichTextEditorMark): Range | null {
            return BitRichTextEditorMarks.apply(root, range, mark, !BitRichTextEditorMarks.isActive(root, range, mark));
        }

        // Puts the mark on every text of the range (on) or takes it off every one (off).
        public static apply(root: Node, range: Range, mark: BitRichTextEditorMark, on: boolean): Range | null {
            if (range.collapsed) return null;
            let texts = BitRichTextEditorMarks.split(root, range);
            if (mark === 'inlineCode') texts = texts.filter(t => !BitRichTextEditorMarks.inCodeBlock(root, t));
            if (texts.length === 0) return null;

            for (const text of texts) {
                if (on) BitRichTextEditorMarks.markText(root, text, mark);
                else BitRichTextEditorMarks.unmarkText(root, text, mark);
            }

            BitRichTextEditorMarks.tidy(root, texts);
            return BitRichTextEditorMarks.cover(texts);
        }

        // Unwraps every inline formatting element over the range's text and drops the inline styles there, leaving
        // links, mentions and code blocks. Returns the range the selection should then cover, or null.
        public static removeFormat(root: Node, range: Range): Range | null {
            if (range.collapsed) return null;
            const texts = BitRichTextEditorMarks.split(root, range);
            if (texts.length === 0) return null;

            for (const text of texts) {
                for (let el = BitRichTextEditorMarks.formattingOf(root, text); el; el = BitRichTextEditorMarks.formattingOf(root, text)) {
                    BitRichTextEditorMarks.unwrap(BitRichTextEditorMarks.isolate(el, text));
                }
            }

            BitRichTextEditorMarks.tidy(root, texts);
            return BitRichTextEditorMarks.cover(texts);
        }

        // Types text at a collapsed range with marks switched on (true) or off (false) for it - the marks a toolbar
        // button or a shortcut set at the caret before anything was typed. The text goes into a node of its own,
        // wrapped in the marks turned on and taken out of the ones turned off, and in a span of the styles set there.
        // Returns the caret after it.
        public static insertText(root: Node, range: Range, text: string, marks: { [mark: string]: boolean }, styles?: { [property: string]: string }): Range | null {
            if (!range.collapsed || !text || !root.contains(range.startContainer)) return null;
            const node = document.createTextNode(text);
            const container = range.startContainer;
            const offset = range.startOffset;
            if (container.nodeType === 3) {
                const t = container as Text;
                if (offset <= 0) t.before(node);
                else if (offset >= t.length) t.after(node);
                else t.splitText(offset).before(node);
            } else {
                container.insertBefore(node, container.childNodes[offset] || null);
            }

            for (const name of Object.keys(marks)) {
                if (!BitRichTextEditorMarks.isMark(name)) continue;
                if (marks[name]) BitRichTextEditorMarks.markText(root, node, name);
                else BitRichTextEditorMarks.unmarkText(root, node, name);
            }
            if (styles && Object.keys(styles).length > 0) BitRichTextEditorMarks.wrapStyled(node, styles);

            const caret = document.createRange();
            caret.setStart(node, node.length);
            caret.collapse(true);
            return caret;
        }

        // The position of a range boundary as a count of characters of text from the start of root: the same for every
        // equivalent way of writing one place, so it tells whether the caret has really moved.
        public static textOffset(root: Node, container: Node, offset: number): number {
            const probe = document.createRange();
            probe.setStart(root, 0);
            try { probe.setEnd(container, offset); } catch { return -1; }
            return probe.toString().length;
        }

        // The range between two text offsets of root, as textOffset counts them: it starts in the text after a boundary
        // between two text nodes and ends in the text before one, so it holds those characters and no node around them.
        public static rangeAt(root: Node, start: number, end: number): Range | null {
            if (end <= start) return null;
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            const range = document.createRange();
            let seen = 0, started = false;
            for (let n = walker.nextNode() as Text | null; n; n = walker.nextNode() as Text | null) {
                const next = seen + n.length;
                if (!started && start < next) {
                    range.setStart(n, start - seen);
                    started = true;
                }
                if (started && end <= next) {
                    range.setEnd(n, end - seen);
                    return range;
                }
                seen = next;
            }
            return null;
        }

        // ---- styles ----

        // The styles the color and font commands write, each on a span of its own: color, background-color,
        // font-family and font-size.
        public static readonly STYLES = ['color', 'background-color', 'font-family', 'font-size'];

        // The value as the browser writes it into a style (a color comes back as rgb()), or '' when it is not a valid
        // value of the property.
        public static styleValue(property: string, value: string): string {
            if (!BitRichTextEditorMarks.STYLES.includes(property) || !value) return '';
            const probe = document.createElement('span');
            probe.style.setProperty(property, value);
            return probe.style.getPropertyValue(property);
        }

        // Paints every text of the range with the style (value), or takes the style off every one (null). The style
        // comes off what painted the texts before - an inline element the range covers part of is split around them,
        // and any element the range holds all the text of loses it - and a value goes on a span around each text,
        // the spans side by side then joined. Returns the range the selection should then cover, or null.
        public static applyStyle(root: Node, range: Range, property: string, value: string | null): Range | null {
            if (range.collapsed || !BitRichTextEditorMarks.STYLES.includes(property)) return null;
            const texts = BitRichTextEditorMarks.split(root, range);
            if (texts.length === 0) return null;
            const inRange = new Set<Node>(texts);

            const around = new Set<HTMLElement>();
            for (const t of texts) {
                for (let n = t.parentNode; n && n !== root; n = n.parentNode) around.add(n as HTMLElement);
            }
            around.forEach(el => {
                if (!el.isConnected || !BitRichTextEditorMarks.paints(el, property)) return;
                if (BitRichTextEditorMarks.textsOf(el).every(t => inRange.has(t))) BitRichTextEditorMarks.unpaint(el, property);
            });
            for (const t of texts) {
                for (let el = BitRichTextEditorMarks.paintOf(root, t, property); el; el = BitRichTextEditorMarks.paintOf(root, t, property)) {
                    BitRichTextEditorMarks.unpaint(BitRichTextEditorMarks.isolate(el, t), property);
                }
            }

            // The span goes inside a link, whose own color would otherwise win over it.
            if (value) {
                for (const top of BitRichTextEditorMarks.outermostOf(root, texts, el => el.nodeName !== 'A')) {
                    if (top.nodeName === 'SPAN' && Array.from((top as Element).attributes).every(a => a.name === 'style')) {
                        (top as HTMLElement).style.setProperty(property, value);
                    } else {
                        BitRichTextEditorMarks.wrapStyled(top, { [property]: value });
                    }
                }
            }

            BitRichTextEditorMarks.tidy(root, texts);
            return BitRichTextEditorMarks.cover(texts);
        }

        // The outermost nodes holding the units and nothing else, each once and in order: a unit, or the inline element
        // around it whose every text and image is a unit, as far up as the elements let it go (through) and short of a
        // block.
        private static outermostOf(root: Node, units: Node[], through: (el: Element) => boolean): Node[] {
            const inRange = new Set<Node>(units);
            const covered = (el: Element) => {
                const parts: Node[] = [...BitRichTextEditorMarks.textsOf(el), ...Array.from(el.querySelectorAll('img'))];
                return parts.length > 0 && parts.every(p => inRange.has(p));
            };
            const tops: Node[] = [];
            for (const unit of units) {
                let top: Node = unit;
                for (let p = top.parentNode; p && p !== root && p.nodeType === 1 && !BitRichTextEditorMarks.BLOCK.test(p.nodeName)
                    && through(p as Element) && covered(p as Element); p = p.parentNode) {
                    top = p;
                }
                if (!tops.includes(top)) tops.push(top);
            }
            return tops;
        }

        // The style at a position, as the toolbar shows it: the computed value on the element there. The background is
        // the nearest one painted below root, and '' where none is.
        public static styleAt(root: Node, range: Range, property: string): string {
            // A selection reads its first text, which a selection starting at the very end of the text before it is not.
            const first = range.collapsed ? null : BitRichTextEditorMarks.textsIn(root, range)[0];
            const at = first ? first.node : BitRichTextEditorMarks.caretNode(range);
            let el: Node | null = at && at.nodeType === 3 ? at.parentNode : at;
            if (!el || !root.contains(el) || el.nodeType !== 1) el = root;
            if (property !== 'background-color') return getComputedStyle(el as Element).getPropertyValue(property);
            for (let n: Node | null = el; n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                const value = getComputedStyle(n as Element).backgroundColor;
                if (value && value !== 'transparent' && !/^rgba\(.*,\s*0\)$/.test(value)) return value;
            }
            return '';
        }

        private static paints(el: Element, property: string): boolean {
            const style = (el as HTMLElement).style;
            return !!style && (!!style.getPropertyValue(property)
                || (property === 'background-color' && !!style.getPropertyValue('background')));
        }

        // The nearest inline element over the node that paints it with the style.
        private static paintOf(root: Node, node: Node, property: string): HTMLElement | null {
            for (let n: Node | null = node.parentNode; n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                if (BitRichTextEditorMarks.BLOCK.test(n.nodeName)) return null;
                if (BitRichTextEditorMarks.paints(n as Element, property)) return n as HTMLElement;
            }
            return null;
        }

        // Takes the style off the element: the background shorthand with the background color, the style attribute once
        // it is empty, and a span that held nothing but the style.
        private static unpaint(el: HTMLElement, property: string) {
            el.style.removeProperty(property);
            if (property === 'background-color') el.style.removeProperty('background');
            if (el.style.length > 0) return;
            el.removeAttribute('style');
            if (el.nodeName === 'SPAN' && el.attributes.length === 0) BitRichTextEditorMarks.unwrap(el);
        }

        private static wrapStyled(node: Node, styles: { [property: string]: string }) {
            const span = document.createElement('span');
            for (const property of Object.keys(styles)) span.style.setProperty(property, styles[property]);
            node.parentNode!.insertBefore(span, node);
            span.appendChild(node);
        }

        private static textsOf(el: Node): Text[] {
            const out: Text[] = [];
            const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
            for (let n = walker.nextNode() as Text | null; n; n = walker.nextNode() as Text | null) {
                if (n.length > 0) out.push(n);
            }
            return out;
        }

        // ---- links ----

        // Links what the range holds to one address: per block, one anchor around the selected inline content, put
        // around the outermost elements the range holds all of (<a><b>al</b>pha</a>), and taking the place of the
        // links that were there. Returns the range the selection should then cover, or null when nothing was linked.
        public static link(root: Node, range: Range, href: string): Range | null {
            if (range.collapsed || !href) return null;
            const images = (Array.from((root as Element).querySelectorAll('img')) as HTMLElement[]).filter(img => {
                const r = document.createRange();
                r.selectNode(img);
                return range.compareBoundaryPoints(Range.START_TO_START, r) <= 0 && range.compareBoundaryPoints(Range.END_TO_END, r) >= 0;
            });
            const units: Node[] = [...BitRichTextEditorMarks.split(root, range), ...images]
                .sort((a, b) => a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1);
            if (units.length === 0) return null;
            for (const unit of units) {
                for (let a = BitRichTextEditorMarks.anchorOf(root, unit); a; a = BitRichTextEditorMarks.anchorOf(root, unit)) {
                    BitRichTextEditorMarks.unwrap(BitRichTextEditorMarks.isolate(a, unit));
                }
            }

            const made: HTMLElement[] = [];
            for (const top of BitRichTextEditorMarks.outermostOf(root, units, () => true)) {
                const a = document.createElement('a');
                a.setAttribute('href', href);
                top.parentNode!.insertBefore(a, top);
                a.appendChild(top);
                made.push(a);
            }
            for (const a of made) {
                while (a.isConnected) {
                    const next = a.nextSibling;
                    if (!next || !made.includes(next as HTMLElement)) break;
                    a.append(...Array.from(next.childNodes));
                    (next as HTMLElement).remove();
                }
            }

            const texts = units.filter(u => u.nodeType === 3) as Text[];
            if (texts.length > 0) BitRichTextEditorMarks.tidy(root, texts);
            const next = document.createRange();
            const first = units[0], last = units[units.length - 1];
            if (first.nodeType === 3) next.setStart(first, 0); else next.setStartBefore(first);
            if (last.nodeType === 3) next.setEnd(last, (last as Text).length); else next.setEndAfter(last);
            return next;
        }

        // Unwraps every link the range touches, whole; at a caret, the link it is in. Returns the range over the same
        // text afterwards, or null when there was no link.
        public static unlink(root: Node, range: Range): Range | null {
            const anchors = range.collapsed
                ? [BitRichTextEditorMarks.anchorOf(root, BitRichTextEditorMarks.caretNode(range) || range.startContainer)].filter(a => !!a) as HTMLElement[]
                : (Array.from((root as Element).querySelectorAll('a')) as HTMLElement[]).filter(a => range.intersectsNode(a));
            const own = BitRichTextEditorMarks.anchorOf(root, range.startContainer);
            if (own && !anchors.includes(own)) anchors.push(own);
            if (anchors.length === 0) return null;

            const start = BitRichTextEditorMarks.textOffset(root, range.startContainer, range.startOffset);
            const end = BitRichTextEditorMarks.textOffset(root, range.endContainer, range.endOffset);
            anchors.forEach(a => BitRichTextEditorMarks.unwrap(a));

            const next = BitRichTextEditorMarks.rangeAt(root, start, Math.max(end, start + 1))
                || BitRichTextEditorMarks.rangeAt(root, Math.max(0, start - 1), start);
            if (!next) return null;
            if (end <= start) next.collapse(BitRichTextEditorMarks.rangeAt(root, start, start + 1) !== null);
            return next;
        }

        private static anchorOf(root: Node, node: Node): HTMLElement | null {
            for (let n: Node | null = node.nodeType === 1 ? node : node.parentNode; n && n !== root; n = n.parentNode) {
                if (n.nodeName === 'A') return n as HTMLElement;
            }
            return null;
        }

        // ---- helpers ----

        // The text node a caret reads its marks from: the one it is in, else the one just before it, else after it.
        private static caretNode(range: Range): Node | null {
            const c = range.startContainer;
            if (c.nodeType === 3) return c;
            const before = c.childNodes[range.startOffset - 1];
            if (before) return BitRichTextEditorMarks.edgeText(before, false) || before;
            const after = c.childNodes[range.startOffset];
            if (after) return BitRichTextEditorMarks.edgeText(after, true) || after;
            return c;
        }

        private static edgeText(node: Node, first: boolean): Node | null {
            if (node.nodeType === 3) return node;
            const kids = Array.from(node.childNodes);
            if (!first) kids.reverse();
            for (const k of kids) {
                const t = BitRichTextEditorMarks.edgeText(k, first);
                if (t) return t;
            }
            return null;
        }

        // The text nodes the range holds some of, with the part it holds.
        private static textsIn(root: Node, range: Range): { node: Text, start: number, end: number }[] {
            const out: { node: Text, start: number, end: number }[] = [];
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            for (let n = walker.nextNode() as Text | null; n; n = walker.nextNode() as Text | null) {
                if (!range.intersectsNode(n) || BitRichTextEditorMarks.isStructuralWhitespace(root, n)) continue;
                const start = n === range.startContainer ? range.startOffset : 0;
                const end = n === range.endContainer ? range.endOffset : n.length;
                if (end > start) out.push({ node: n, start, end });
            }
            return out;
        }

        // The texts a mark's state is judged on: whitespace does not count where there is anything else.
        private static counted(texts: Text[]): Text[] {
            const solid = texts.filter(t => /\S/.test(t.data));
            return solid.length > 0 ? solid : texts;
        }

        private static isStructuralWhitespace(root: Node, n: Text): boolean {
            if (/\S/.test(n.data)) return false;
            const parent = n.parentNode;
            if (!parent || parent === root || BitRichTextEditorMarks.STRUCTURAL.test(parent.nodeName)) return true;
            const isBlock = (s: Node | null) => !!s && s.nodeType === 1 && BitRichTextEditorMarks.BLOCK.test(s.nodeName);
            return isBlock(n.previousSibling) || isBlock(n.nextSibling);
        }

        // Splits the text at the range's edges, so the range holds whole text nodes, and returns them in order.
        private static split(root: Node, range: Range): Text[] {
            return BitRichTextEditorMarks.textsIn(root, range).map(({ node, start, end }) => {
                let t = node;
                if (end < t.length) t.splitText(end);
                if (start > 0) t = t.splitText(start);
                return t;
            });
        }

        private static markText(root: Node, text: Text, mark: BitRichTextEditorMark) {
            const other = BitRichTextEditorMarks.EXCLUDES[mark] as BitRichTextEditorMark | undefined;
            if (other) BitRichTextEditorMarks.unmarkText(root, text, other);
            if (BitRichTextEditorMarks.markOf(root, text, mark)) return;
            if (mark === 'inlineCode' && BitRichTextEditorMarks.inCodeBlock(root, text)) return;
            const el = document.createElement(BitRichTextEditorMarks.WRITES[mark]);
            text.before(el);
            el.appendChild(text);
        }

        private static unmarkText(root: Node, text: Text, mark: BitRichTextEditorMark) {
            for (let el = BitRichTextEditorMarks.markOf(root, text, mark); el; el = BitRichTextEditorMarks.markOf(root, text, mark)) {
                BitRichTextEditorMarks.unwrap(BitRichTextEditorMarks.isolate(el, text));
            }
        }

        // The nearest inline formatting element over the node that clearing the formatting unwraps.
        private static formattingOf(root: Node, node: Node): HTMLElement | null {
            for (let n: Node | null = node.parentNode; n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                const el = n as HTMLElement;
                if (BitRichTextEditorMarks.BLOCK.test(el.nodeName)) return null;
                if (!BitRichTextEditorMarks.FORMATTING.test(el.nodeName)) continue;
                if (el.nodeName === 'CODE' && el.parentNode && el.parentNode.nodeName === 'PRE') return null;
                if (el.nodeName === 'MARK' && el.classList.contains('bit-rte-find')) continue;
                if (el.nodeName === 'SPAN' && Array.from(el.attributes).some(a => a.name !== 'style')) continue;
                return el;
            }
            return null;
        }

        // Splits el around the node inside it: what came before the node and what comes after it go into copies of el
        // of their own (with copies of the elements between), so el holds the node alone and can be unwrapped without
        // taking its mark off anything else. Returns el.
        private static isolate(el: HTMLElement, node: Node): HTMLElement {
            const parent = el.parentNode!;
            const head = document.createRange();
            head.setStart(el, 0);
            head.setEndBefore(node);
            const before = head.extractContents();
            if (BitRichTextEditorMarks.hasContent(before)) {
                const copy = el.cloneNode(false);
                copy.appendChild(before);
                parent.insertBefore(copy, el);
            }
            const tail = document.createRange();
            tail.setStartAfter(node);
            tail.setEnd(el, el.childNodes.length);
            const after = tail.extractContents();
            if (BitRichTextEditorMarks.hasContent(after)) {
                const copy = el.cloneNode(false);
                copy.appendChild(after);
                parent.insertBefore(copy, el.nextSibling);
            }
            return el;
        }

        private static unwrap(el: HTMLElement) {
            el.replaceWith(...Array.from(el.childNodes));
        }

        private static hasContent(node: Node): boolean {
            if ((node.textContent || '').length > 0) return true;
            return node.nodeType === 11 || node.nodeType === 1
                ? !!(node as Element | DocumentFragment).querySelector(BitRichTextEditorMarks.VOID)
                : false;
        }

        // Joins the marks the edit left side by side (<b>al</b><b>pha</b> is <b>alpha</b>) and drops the formatting
        // elements it emptied, in the blocks the texts are in. The text nodes are only ever moved, never merged, so the
        // ones the selection is put back on stay where they are.
        private static tidy(root: Node, texts: Text[]) {
            const scopes = new Set<Node>();
            for (const t of texts) scopes.add(BitRichTextEditorMarks.blockOf(root, t));
            const formatting = (scope: Node) => (Array.from((scope as Element).querySelectorAll('*')) as HTMLElement[])
                .filter(el => BitRichTextEditorMarks.FORMATTING.test(el.nodeName));
            scopes.forEach(scope => {
                // Innermost first, so an element holding only emptied ones goes too.
                formatting(scope).reverse().forEach(el => {
                    if (!BitRichTextEditorMarks.hasContent(el)) el.remove();
                });
                // In document order, so the outer marks merge first and bring their inner ones side by side for the
                // inner ones' turn (<b><i>a</i></b><b><i>b</i></b> is <b><i>ab</i></b>).
                formatting(scope).forEach(el => {
                    while (el.isConnected) {
                        const next = el.nextSibling;
                        if (!next || next.nodeType !== 1 || !BitRichTextEditorMarks.sameElement(el, next as HTMLElement)) break;
                        el.append(...Array.from(next.childNodes));
                        (next as HTMLElement).remove();
                    }
                });
            });
        }

        private static blockOf(root: Node, node: Node): Node {
            for (let n: Node | null = node.parentNode; n && n !== root; n = n.parentNode) {
                if (BitRichTextEditorMarks.BLOCK.test(n.nodeName)) return n;
            }
            return root;
        }

        private static sameElement(a: HTMLElement, b: HTMLElement): boolean {
            if (a.nodeName !== b.nodeName || a.attributes.length !== b.attributes.length) return false;
            if (a.nodeName === 'MARK' && a.classList.contains('bit-rte-find')) return false;
            return Array.from(a.attributes).every(attr => b.getAttribute(attr.name) === attr.value);
        }

        private static cover(texts: Text[]): Range {
            const range = document.createRange();
            const first = texts[0], last = texts[texts.length - 1];
            range.setStart(first, 0);
            range.setEnd(last, last.length);
            return range;
        }
    }
}
