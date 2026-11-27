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
    // wraps them, and is taken off again by applying it once more or by turning them back into paragraphs. Lists,
    // indentation and alignment work on the same lines.

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

            // A surface emptied down to nothing (Gecko leaves no block behind) gets the block to format.
            if (BitRichTextEditorBlocks.isBare(root)) {
                const block = document.createElement(tag === 'blockquote' ? 'p' : tag);
                block.appendChild(document.createElement('br'));
                root.replaceChildren(block);
                const caret = document.createRange();
                caret.setStart(block, 0);
                if (tag === 'blockquote') BitRichTextEditorBlocks.quote(root, [block]);
                return caret;
            }

            return BitRichTextEditorBlocks.edit(root, range, (lines, moved) => {
                const quoted = (line: BitRichTextEditorLine) => !!BitRichTextEditorBlocks.quoteOf(root, BitRichTextEditorBlocks.lineNode(line));
                if (tag === 'blockquote') {
                    if (lines.every(quoted)) BitRichTextEditorBlocks.unquote(root, lines);
                    else BitRichTextEditorBlocks.quote(root, lines.map(BitRichTextEditorBlocks.lineNode));
                    return;
                }
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
            });
        }

        // The list the position is in, as the toolbar shows it: "ol", "ul", "tasks" (a checklist), or empty.
        public static listState(root: Node, range: Range): string {
            const li = BitRichTextEditorBlocks.itemOf(root, BitRichTextEditorBlocks.nodeAt(range.startContainer, range.startOffset));
            if (!li) return '';
            const list = li.parentElement!;
            if (list.nodeName === 'OL') return 'ol';
            return list.classList.contains('bit-rte-tasks') ? 'tasks' : 'ul';
        }

        // Turns the lines the range touches into the items of one list of the kind (ol or ul), or back into
        // paragraphs where they all are items of that kind already. Items of a list of the other kind switch that list.
        public static toggleList(root: HTMLElement, range: Range, kind: 'ol' | 'ul'): Range | null {
            if (BitRichTextEditorBlocks.isBare(root)) root.replaceChildren(BitRichTextEditorBlocks.emptyParagraph());
            return BitRichTextEditorBlocks.edit(root, range, (lines, moved) => {
                const items = lines.map(line => BitRichTextEditorBlocks.itemOf(root, BitRichTextEditorBlocks.lineNode(line)));
                const isKind = (li: HTMLElement | null) => !!li && li.parentNode!.nodeName === kind.toUpperCase()
                    && !(kind === 'ul' && (li.parentNode as HTMLElement).classList.contains('bit-rte-tasks'));

                if (items.every(isKind)) {
                    for (const li of BitRichTextEditorBlocks.outermost(items)) BitRichTextEditorBlocks.unlist(root, li, moved);
                    return;
                }

                // The lists of the other kind switch, all of each.
                const lists = new Set<HTMLElement>();
                items.forEach(li => { if (li && !isKind(li)) lists.add(li.parentNode as HTMLElement); });
                lists.forEach(list => moved.set(list, BitRichTextEditorBlocks.retagList(list, kind)));

                // The rest become items, the ones side by side in one list, which joins a list of the kind it touches.
                let current: HTMLElement | null = null;
                const made: HTMLElement[] = [];
                lines.forEach((line, i) => {
                    if (items[i]) return;
                    const first = 'leaf' in line ? line.leaf : line.run[0];
                    if (!current || BitRichTextEditorBlocks.nextOf(current) !== first || !('leaf' in line)) {
                        current = document.createElement(kind);
                        first.parentNode!.insertBefore(current, first);
                        made.push(current);
                    }
                    const li = document.createElement('li');
                    current.appendChild(li);
                    if (!('leaf' in line)) li.append(...line.run);
                    else if (/^(P|DIV)$/.test(line.leaf.nodeName)) {
                        BitRichTextEditorBlocks.copyKept(line.leaf, li);
                        li.append(...Array.from(line.leaf.childNodes));
                        line.leaf.remove();
                        moved.set(line.leaf, li);
                    } else {
                        li.appendChild(line.leaf);
                    }
                });
                made.forEach(list => BitRichTextEditorBlocks.joinNeighbours(list));
            });
        }

        // Nests each item the range touches under the item before it (an item with none before it stays), and gives
        // any other line one more step of indentation.
        public static indent(root: HTMLElement, range: Range): Range | null {
            return BitRichTextEditorBlocks.edit(root, range, lines => {
                const items = lines.map(line => BitRichTextEditorBlocks.itemOf(root, BitRichTextEditorBlocks.lineNode(line)));
                for (const li of BitRichTextEditorBlocks.outermost(items)) {
                    const previous = li.previousElementSibling;
                    if (!previous || previous.nodeName !== 'LI') continue;
                    const list = li.parentNode as HTMLElement;
                    let sub = previous.lastElementChild as HTMLElement | null;
                    if (!sub || sub.nodeName !== list.nodeName) {
                        sub = list.cloneNode(false) as HTMLElement;
                        previous.appendChild(sub);
                    }
                    sub.appendChild(li);
                }
                lines.forEach((line, i) => {
                    if (!items[i] && 'leaf' in line) BitRichTextEditorBlocks.step(line.leaf, 1);
                });
            });
        }

        // Brings each item the range touches out one level (out of the list at the top level), and takes a step of
        // indentation off any other line.
        public static outdent(root: HTMLElement, range: Range): Range | null {
            return BitRichTextEditorBlocks.edit(root, range, (lines, moved) => {
                const items = lines.map(line => BitRichTextEditorBlocks.itemOf(root, BitRichTextEditorBlocks.lineNode(line)));
                for (const li of BitRichTextEditorBlocks.outermost(items)) {
                    if (BitRichTextEditorBlocks.isNested(root, li)) BitRichTextEditorBlocks.lift(li);
                    else BitRichTextEditorBlocks.unlist(root, li, moved);
                }
                lines.forEach((line, i) => {
                    if (!items[i] && 'leaf' in line) BitRichTextEditorBlocks.step(line.leaf, -1);
                });
            });
        }

        // Runs an edit of the lines the range touches and returns the range over the same content afterwards. Text a
        // quote or the surface holds directly is made a paragraph first: the edits move blocks.
        private static edit(root: HTMLElement, range: Range, change: (lines: BitRichTextEditorLine[], moved: Map<Node, Node>) => void): Range | null {
            const start = BitRichTextEditorBlocks.hold(range.startContainer, range.startOffset);
            const end = BitRichTextEditorBlocks.hold(range.endContainer, range.endOffset);
            const moved = new Map<Node, Node>();

            let lines = BitRichTextEditorBlocks.linesIn(root, range);
            if (lines.length === 0) return null;
            lines = lines.map(line => {
                if ('leaf' in line) return line;
                const parent = line.run[0].parentNode!;
                if (parent !== root && parent.nodeName !== 'BLOCKQUOTE') return line;
                const p = document.createElement('p');
                parent.insertBefore(p, line.run[0]);
                p.append(...line.run);
                return { leaf: p };
            });

            change(lines, moved);

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

        private static isBare(root: Node): boolean {
            return !Array.from(root.childNodes).some(n => n.nodeType === 1 || /\S/.test(n.nodeValue || ''));
        }

        private static emptyParagraph(): HTMLElement {
            const p = document.createElement('p');
            p.appendChild(document.createElement('br'));
            return p;
        }

        // ---- lists ----

        private static itemOf(root: Node, node: Node): HTMLElement | null {
            for (let n: Node | null = node; n && n !== root; n = n.parentNode) {
                if (n.nodeName === 'LI' && n.parentNode && /^(UL|OL)$/.test(n.parentNode.nodeName)) return n as HTMLElement;
            }
            return null;
        }

        // The distinct items, in order, less the ones inside another of them (which move along with it).
        private static outermost(items: (HTMLElement | null)[]): HTMLElement[] {
            const set = Array.from(new Set(items.filter((li): li is HTMLElement => !!li)));
            return set.filter(li => !set.some(other => other !== li && other.contains(li)));
        }

        private static isNested(root: Node, li: HTMLElement): boolean {
            const holder = li.parentNode!.parentNode;
            return !!holder && holder !== root && holder.nodeName === 'LI';
        }

        // Moves a nested item out to the level of the item it was nested under, right after it; the items after it in
        // its list stay after it, nested under it now.
        private static lift(li: HTMLElement) {
            const list = li.parentNode as HTMLElement;
            const holder = list.parentNode as HTMLElement;
            if (li.nextElementSibling) {
                let rest = li.lastElementChild as HTMLElement | null;
                if (!rest || rest.nodeName !== list.nodeName) {
                    rest = list.cloneNode(false) as HTMLElement;
                    li.appendChild(rest);
                }
                while (li.nextSibling) rest.appendChild(li.nextSibling);
            }
            holder.after(li);
            if (!BitRichTextEditorBlocks.hasContent(list)) list.remove();
        }

        // Takes an item out of its lists altogether: lifted to the top level, then the list is split around it and
        // its text becomes a paragraph (its blocks and nested lists stay as they are).
        private static unlist(root: Node, li: HTMLElement, moved: Map<Node, Node>) {
            while (BitRichTextEditorBlocks.isNested(root, li)) BitRichTextEditorBlocks.lift(li);
            const list = li.parentNode as HTMLElement;
            if (li.nextElementSibling) {
                const tail = list.cloneNode(false) as HTMLElement;
                while (li.nextSibling) tail.appendChild(li.nextSibling);
                list.after(tail);
            }
            const out: Node[] = [];
            let run: Node[] = [];
            const flush = () => {
                if (run.length === 0) return;
                const p = document.createElement('p');
                BitRichTextEditorBlocks.copyKept(li, p);
                p.append(...run);
                out.push(p);
                run = [];
            };
            for (const n of Array.from(li.childNodes)) {
                if (n.nodeType === 1 && BitRichTextEditorBlocks.BLOCK.test(n.nodeName)) {
                    flush();
                    out.push(n);
                } else if (n.nodeType === 1 || /\S/.test(n.nodeValue || '') || run.length > 0) {
                    run.push(n);
                }
            }
            flush();
            if (out.length === 0) out.push(BitRichTextEditorBlocks.emptyParagraph());
            list.after(...out);
            moved.set(li, out[0]);
            li.remove();
            if (!BitRichTextEditorBlocks.hasContent(list)) list.remove();
        }

        // A list given the other kind: a checklist turned into a numbered or a plain list is no checklist any more.
        private static retagList(list: HTMLElement, kind: string): HTMLElement {
            const next = document.createElement(kind);
            for (const attr of Array.from(list.attributes)) next.setAttribute(attr.name, attr.value);
            next.classList.remove('bit-rte-tasks');
            if (next.classList.length === 0) next.removeAttribute('class');
            Array.from(list.children).forEach(li => li.removeAttribute('data-checked'));
            next.append(...Array.from(list.childNodes));
            list.replaceWith(next);
            return next;
        }

        // Joins a list with a list of the same kind right before or after it.
        private static joinNeighbours(list: HTMLElement) {
            const same = (other: Node | null): other is HTMLElement => !!other && other.nodeType === 1
                && other.nodeName === list.nodeName && (other as HTMLElement).className === list.className;
            const before = BitRichTextEditorBlocks.previousOf(list);
            if (same(before)) {
                before.append(...Array.from(list.childNodes));
                list.remove();
                list = before;
            }
            const after = BitRichTextEditorBlocks.nextOf(list);
            if (same(after)) {
                list.append(...Array.from(after.childNodes));
                after.remove();
            }
        }

        private static nextOf(node: Node): Node | null {
            let n = node.nextSibling;
            while (n && n.nodeType === 3 && !/\S/.test(n.nodeValue || '')) n = n.nextSibling;
            return n;
        }

        private static previousOf(node: Node): Node | null {
            let n = node.previousSibling;
            while (n && n.nodeType === 3 && !/\S/.test(n.nodeValue || '')) n = n.previousSibling;
            return n;
        }

        private static copyKept(from: Element, to: Element) {
            for (const name of BitRichTextEditorBlocks.KEPT) {
                const value = from.getAttribute(name);
                if (value !== null) to.setAttribute(name, value);
            }
        }

        // ---- indentation ----

        // One step of indentation is 40px of margin on the block's starting side; a step taken off the last one
        // leaves no margin at all.
        public static readonly INDENT_STEP = 40;

        private static step(block: HTMLElement, by: number) {
            const side = getComputedStyle(block).direction === 'rtl' ? 'margin-right' : 'margin-left';
            const current = parseFloat(block.style.getPropertyValue(side)) || 0;
            const next = Math.max(0, current + by * BitRichTextEditorBlocks.INDENT_STEP);
            if (next > 0) block.style.setProperty(side, next + 'px');
            else block.style.removeProperty(side);
            if (block.style.length === 0) block.removeAttribute('style');
        }

        // ---- alignment ----

        // The alignments the justify commands write, as text-align values.
        public static readonly ALIGNMENTS = ['left', 'center', 'right', 'justify'];

        // The alignment of the line at a position, as it is laid out: "left", "center", "right" or "justify" (a start or
        // end alignment resolved through the line's direction). Empty outside any line.
        public static alignState(root: HTMLElement, range: Range): string {
            const line = BitRichTextEditorBlocks.lineOf(root, BitRichTextEditorBlocks.nodeAt(range.startContainer, range.startOffset));
            const holder = line ? BitRichTextEditorBlocks.alignedOf(root, line) : root;
            return BitRichTextEditorBlocks.resolvedAlign(holder);
        }

        // Aligns every line the range touches: a text-align style on its block, or none where the block is laid out
        // that way without one (a left alignment in a left-to-right block). A presentational align attribute goes.
        public static align(root: HTMLElement, range: Range, value: string): Range | null {
            if (!BitRichTextEditorBlocks.ALIGNMENTS.includes(value)) return null;
            if (BitRichTextEditorBlocks.isBare(root)) root.replaceChildren(BitRichTextEditorBlocks.emptyParagraph());
            return BitRichTextEditorBlocks.edit(root, range, lines => {
                for (const line of lines) {
                    const block = BitRichTextEditorBlocks.alignedOf(root, line);
                    if (block === root) continue;
                    block.removeAttribute('align');
                    block.style.removeProperty('text-align');
                    if (BitRichTextEditorBlocks.resolvedAlign(block) !== value) block.style.setProperty('text-align', value);
                    if (block.style.length === 0) block.removeAttribute('style');
                }
            });
        }

        // The element a line's alignment is written on: its leaf block, or the block holding its run.
        private static alignedOf(root: HTMLElement, line: BitRichTextEditorLine): HTMLElement {
            return 'leaf' in line ? line.leaf : (line.run[0].parentNode as HTMLElement) || root;
        }

        private static resolvedAlign(el: Element): string {
            const style = getComputedStyle(el);
            const rtl = style.direction === 'rtl';
            const value = style.textAlign.replace(/^-(webkit|moz)-/, '');
            switch (value) {
                case 'start': return rtl ? 'right' : 'left';
                case 'end': return rtl ? 'left' : 'right';
                case 'left': case 'center': case 'right': case 'justify': return value;
                default: return rtl ? 'right' : 'left';
            }
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
            BitRichTextEditorBlocks.copyKept(leaf, el);
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
