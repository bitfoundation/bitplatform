namespace BitBlazorUI {
    // bit-trim: on-demand
    // The block formats of BitRichTextEditor (BitRichTextEditor.ts): paragraphs, headings, code blocks and quotes. They
    // used to be document.execCommand('formatBlock'), which each engine answers with other markup (Blink turns a
    // paragraph into a bare <blockquote>, Gecko nests or splits it); here they are DOM edits of the editor's own, so the
    // same command leaves the same markup in Blink, Gecko and WebKit. Everything works on a root element and a Range and
    // keeps no state.
    //
    // A line is a leaf block (p, h1-h6, pre, div, address) or, where a container holds its text directly (a list item,
    // a table cell), the run of inline content it holds. A format retags the leaf blocks the range touches; a quote
    // wraps them, and is taken off again by applying it once more or by turning them back into paragraphs.

    type BitRichTextEditorLine = { leaf: HTMLElement } | { run: Node[] };

    export class BitRichTextEditorBlocks {
        // The tags formatBlock writes.
        public static readonly FORMATS = ['p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'pre', 'blockquote'];

        private static readonly LEAF = /^(P|H[1-6]|PRE|DIV|ADDRESS)$/;

        private static readonly BLOCK = /^(ADDRESS|ARTICLE|ASIDE|AUDIO|BLOCKQUOTE|DD|DETAILS|DIV|DL|DT|FIGCAPTION|FIGURE|FOOTER|H[1-6]|HEADER|HR|IFRAME|LI|MAIN|NAV|OL|P|PRE|SECTION|SUMMARY|TABLE|TBODY|TD|TFOOT|TH|THEAD|TR|UL|VIDEO)$/;

        // The attributes a block keeps when it is given another tag.
        private static readonly KEPT = ['style', 'dir', 'lang'];

        // The block format at a position, as the toolbar shows it: the leaf block's tag, or "blockquote" for a
        // paragraph in a quote (a quote's lines are paragraphs). Empty outside any of them.
        public static blockState(root: Node, range: Range): string {
            let leaf = '';
            let quote = false;
            for (let n: Node | null = BitRichTextEditorBlocks.nodeAt(range.startContainer, range.startOffset); n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                if (!leaf && BitRichTextEditorBlocks.LEAF.test(n.nodeName) && !BitRichTextEditorBlocks.hasBlockChild(n)) {
                    leaf = n.nodeName.toLowerCase();
                }
                if (n.nodeName === 'BLOCKQUOTE') quote = true;
            }
            return quote && (!leaf || leaf === 'p' || leaf === 'div') ? 'blockquote' : leaf;
        }

        // Applies a block format to every line the range touches. Returns the range the selection should then cover
        // (the same text, in the blocks it is now in), or null when there was nothing to format.
        public static formatBlock(root: HTMLElement, range: Range, tag: string): Range | null {
            tag = (tag || 'p').replace(/[<>\s]/g, '').toLowerCase();
            if (!BitRichTextEditorBlocks.FORMATS.includes(tag)) return null;

            const start = BitRichTextEditorBlocks.hold(range.startContainer, range.startOffset);
            const end = BitRichTextEditorBlocks.hold(range.endContainer, range.endOffset);
            const moved = new Map<Node, Node>();

            // A surface emptied down to nothing (Gecko leaves no block behind) gets the block to format.
            if (!Array.from(root.childNodes).some(n => n.nodeType === 1 || /\S/.test(n.nodeValue || ''))) {
                const block = document.createElement(tag === 'blockquote' ? 'p' : tag);
                block.appendChild(document.createElement('br'));
                root.replaceChildren(block);
                const caret = document.createRange();
                caret.setStart(block, 0);
                if (tag === 'blockquote') BitRichTextEditorBlocks.quote(root, [block]);
                return caret;
            }

            // Text a quote or the surface holds directly is a paragraph first: quoting and unquoting move blocks.
            let lines = BitRichTextEditorBlocks.linesIn(root, range);
            if (lines.length === 0) return null;
            const leaves = lines.map(line => {
                if ('leaf' in line) return line.leaf;
                const parent = line.run[0].parentNode!;
                if (parent !== root && parent.nodeName !== 'BLOCKQUOTE') return null;
                const p = document.createElement('p');
                line.run[0].parentNode!.insertBefore(p, line.run[0]);
                p.append(...line.run);
                return p;
            });
            lines = lines.map((line, i) => leaves[i] ? { leaf: leaves[i]! } : line);

            const quoted = (line: BitRichTextEditorLine) => !!BitRichTextEditorBlocks.quoteOf(root, BitRichTextEditorBlocks.lineNode(line));
            if (tag === 'blockquote') {
                if (lines.every(quoted)) BitRichTextEditorBlocks.unquote(root, lines);
                else BitRichTextEditorBlocks.quote(root, lines.map(BitRichTextEditorBlocks.lineNode));
            } else {
                // Normal text is no quote either: the toolbar's quote button turns a quote off by asking for it.
                if (tag === 'p') BitRichTextEditorBlocks.unquote(root, lines.filter(quoted));
                for (const line of lines) {
                    if ('leaf' in line) {
                        const next = BitRichTextEditorBlocks.retag(line.leaf, tag);
                        if (next !== line.leaf) moved.set(line.leaf, next);
                    } else if (tag !== 'p') {
                        // A list item or a cell is already a line of its own; any other format wraps its text.
                        const el = document.createElement(tag);
                        line.run[0].parentNode!.insertBefore(el, line.run[0]);
                        el.append(...line.run);
                    }
                }
            }

            const from = BitRichTextEditorBlocks.place(start, moved);
            const to = BitRichTextEditorBlocks.place(end, moved);
            if (!from || !to) return null;
            const next = document.createRange();
            try {
                next.setStart(from[0], from[1]);
                next.setEnd(to[0], to[1]);
            } catch {
                next.setStart(from[0], from[1]);
                next.collapse(true);
            }
            return next;
        }

        // ---- lines ----

        // The lines the range touches, in order: a collapsed range touches the line of its caret.
        private static linesIn(root: HTMLElement, range: Range): BitRichTextEditorLine[] {
            const nodes: Node[] = [];
            if (range.collapsed) {
                nodes.push(BitRichTextEditorBlocks.nodeAt(range.startContainer, range.startOffset));
            } else {
                const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT);
                for (let n = walker.nextNode(); n; n = walker.nextNode()) {
                    if (!range.intersectsNode(n)) continue;
                    if (n.nodeType === 3 ? /\S/.test(n.nodeValue || '') && BitRichTextEditorBlocks.overlaps(range, n as Text)
                        : /^(BR|IMG|HR)$/.test(n.nodeName)) {
                        nodes.push(n);
                    }
                }
                if (nodes.length === 0) nodes.push(BitRichTextEditorBlocks.nodeAt(range.startContainer, range.startOffset));
            }

            const lines: BitRichTextEditorLine[] = [];
            const seen = new Set<Node>();
            for (const node of nodes) {
                const line = BitRichTextEditorBlocks.lineOf(root, node);
                if (!line) continue;
                const key = BitRichTextEditorBlocks.lineNode(line);
                if (seen.has(key)) continue;
                seen.add(key);
                lines.push(line);
            }
            return lines;
        }

        private static overlaps(range: Range, text: Text): boolean {
            const start = text === range.startContainer ? range.startOffset : 0;
            const end = text === range.endContainer ? range.endOffset : text.length;
            return end > start;
        }

        // The line a node is on: its leaf block, else the run of inline content around it in the block that holds it.
        private static lineOf(root: HTMLElement, node: Node): BitRichTextEditorLine | null {
            let inline: Node = node;
            for (let n: Node | null = node; n && n !== root; n = n.parentNode) {
                if (n.nodeType !== 1) continue;
                if (BitRichTextEditorBlocks.BLOCK.test(n.nodeName)) {
                    if (BitRichTextEditorBlocks.LEAF.test(n.nodeName) && !BitRichTextEditorBlocks.hasBlockChild(n)) {
                        return { leaf: n as HTMLElement };
                    }
                    if (n === node) return null;
                    return { run: BitRichTextEditorBlocks.runAround(inline) };
                }
                inline = n;
            }
            if (node === root) return null;
            return { run: BitRichTextEditorBlocks.runAround(inline) };
        }

        private static runAround(node: Node): Node[] {
            const isInline = (n: Node | null) => !!n && (n.nodeType === 3 || (n.nodeType === 1 && !BitRichTextEditorBlocks.BLOCK.test(n.nodeName)));
            let first = node, last = node;
            while (isInline(first.previousSibling)) first = first.previousSibling!;
            while (isInline(last.nextSibling)) last = last.nextSibling!;
            const run: Node[] = [];
            for (let n: Node | null = first; n; n = n.nextSibling) {
                run.push(n);
                if (n === last) break;
            }
            return run;
        }

        private static lineNode(line: BitRichTextEditorLine): Node {
            return 'leaf' in line ? line.leaf : line.run[0];
        }

        private static hasBlockChild(n: Node): boolean {
            return Array.from(n.childNodes).some(c => c.nodeType === 1 && BitRichTextEditorBlocks.BLOCK.test(c.nodeName));
        }

        // The node a boundary sits at: the text it is in, else the child after it, else the one before it.
        private static nodeAt(container: Node, offset: number): Node {
            if (container.nodeType === 3) return container;
            return container.childNodes[offset] || container.childNodes[offset - 1] || container;
        }

        // ---- formats ----

        // Gives a leaf block another tag, moving its content into the new element. A code block keeps its lines as
        // line breaks once it is not one, and its single <code> goes.
        private static retag(leaf: HTMLElement, tag: string): HTMLElement {
            if (leaf.nodeName.toLowerCase() === tag) return leaf;
            const el = document.createElement(tag);
            for (const name of BitRichTextEditorBlocks.KEPT) {
                const value = leaf.getAttribute(name);
                if (value !== null) el.setAttribute(name, value);
            }
            if (leaf.nodeName === 'PRE') {
                const only = Array.from(leaf.childNodes).filter(n => n.nodeType === 1 || (n.nodeValue || '') !== '');
                if (only.length === 1 && only[0].nodeName === 'CODE') (only[0] as HTMLElement).replaceWith(...Array.from(only[0].childNodes));
                BitRichTextEditorBlocks.newlinesToBreaks(leaf);
            }
            el.append(...Array.from(leaf.childNodes));
            leaf.replaceWith(el);
            return el;
        }

        private static newlinesToBreaks(block: HTMLElement) {
            const walker = document.createTreeWalker(block, NodeFilter.SHOW_TEXT);
            const texts: Text[] = [];
            for (let n = walker.nextNode(); n; n = walker.nextNode()) texts.push(n as Text);
            const last = texts[texts.length - 1];
            for (const text of texts) {
                let value = text.data;
                // The newline a code block ends with is no line of its own.
                if (text === last) value = value.replace(/\n$/, '');
                if (!value.includes('\n')) {
                    if (value !== text.data) text.data = value;
                    continue;
                }
                const parts = value.split('\n');
                text.data = parts[0];
                let at: Node = text;
                for (const part of parts.slice(1)) {
                    const br = document.createElement('br');
                    (at as ChildNode).after(br);
                    at = br;
                    if (part) {
                        const t = document.createTextNode(part);
                        br.after(t);
                        at = t;
                    }
                }
            }
        }

        private static quoteOf(root: Node, node: Node): HTMLElement | null {
            for (let n: Node | null = node.parentNode; n && n !== root; n = n.parentNode) {
                if (n.nodeName === 'BLOCKQUOTE') return n as HTMLElement;
            }
            return null;
        }

        // Wraps the blocks from the first node's to the last node's, in the block that holds them all, in one quote.
        private static quote(root: HTMLElement, nodes: Node[]) {
            const span = document.createRange();
            span.setStartBefore(nodes[0]);
            span.setEndAfter(nodes[nodes.length - 1]);
            let container: Node = span.commonAncestorContainer;
            const isContainer = (n: Node) => n === root || /^(BLOCKQUOTE|LI|TD|TH|DD|DT)$/.test(n.nodeName)
                || (n.nodeName === 'DIV' && BitRichTextEditorBlocks.hasBlockChild(n));
            while (!isContainer(container)) container = container.parentNode!;
            const childOf = (n: Node) => {
                let c = n;
                while (c.parentNode !== container) c = c.parentNode!;
                return c;
            };
            const first = childOf(nodes[0]);
            const last = childOf(nodes[nodes.length - 1]);
            const quote = document.createElement('blockquote');
            container.insertBefore(quote, first);
            for (let n: Node | null = first; n;) {
                const next: Node | null = n.nextSibling;
                quote.appendChild(n);
                if (n === last) break;
                n = next;
            }
        }

        // Takes the lines out of the quotes they are in: a quote is split around them, the part before and the part
        // after staying quoted.
        private static unquote(root: HTMLElement, lines: BitRichTextEditorLine[]) {
            const groups = new Map<HTMLElement, Node[]>();
            for (const line of lines) {
                const node = BitRichTextEditorBlocks.lineNode(line);
                const quote = BitRichTextEditorBlocks.quoteOf(root, node);
                if (!quote) continue;
                if (!groups.has(quote)) groups.set(quote, []);
                groups.get(quote)!.push(node);
            }
            groups.forEach((nodes, quote) => {
                const childOf = (n: Node) => {
                    let c = n;
                    while (c.parentNode !== quote) c = c.parentNode!;
                    return c;
                };
                const first = childOf(nodes[0]);
                const last = childOf(nodes[nodes.length - 1]);
                const parent = quote.parentNode!;
                const tail = document.createElement('blockquote');
                for (const name of Array.from(quote.attributes)) tail.setAttribute(name.name, name.value);
                while (last.nextSibling) tail.appendChild(last.nextSibling);
                const middle: Node[] = [];
                for (let n: Node | null = first; n;) {
                    const next: Node | null = n.nextSibling;
                    middle.push(n);
                    if (n === last) break;
                    n = next;
                }
                const anchor = quote.nextSibling;
                for (const n of middle) parent.insertBefore(n, anchor);
                if (BitRichTextEditorBlocks.hasContent(tail)) parent.insertBefore(tail, anchor);
                if (!BitRichTextEditorBlocks.hasContent(quote)) quote.remove();
            });
        }

        private static hasContent(el: Element): boolean {
            return Array.from(el.childNodes).some(n => n.nodeType === 1 || /\S/.test(n.nodeValue || ''));
        }

        // ---- selection ----

        // A boundary held by a node that is moved but never rebuilt: a text node with its offset, the child it sits
        // before or after, or the element itself when it is empty (which `moved` then maps to its new element).
        private static hold(container: Node, offset: number): any {
            if (container.nodeType === 3) return { node: container, offset };
            const child = container.childNodes[offset];
            if (child) return { node: child, before: true };
            if (container.lastChild) return { node: container.lastChild, before: false };
            return { node: container, offset: 0 };
        }

        private static place(held: any, moved: Map<Node, Node>): [Node, number] | null {
            const node = moved.get(held.node) || held.node;
            if (!node.isConnected) return null;
            if (held.before === undefined) return [node, held.offset];
            const parent = node.parentNode;
            if (!parent) return null;
            return [parent, Array.prototype.indexOf.call(parent.childNodes, node) + (held.before ? 0 : 1)];
        }
    }
}
