namespace BitBlazorUI {
    // bit-trim: on-demand
    // The insertion of BitRichTextEditor (BitRichTextEditor.ts): markup (a paste, a drop, an image, a table, media, a
    // rule, a link at a caret, an app's InsertHtmlAsync) and text (an emoji, an input rule's replacement). They used to be
    // document.execCommand('insertHTML') and ('insertText'), which each engine answers with other markup - Blink restyles
    // what it inserts after the page's stylesheets and puts a list inside the paragraph it was pasted into - so here they
    // are DOM edits of the editor's own. Everything works on a root element and a Range and keeps no state; what is
    // inserted has been sanitized by the caller.
    //
    // The selection's content is deleted first, and the blocks it went across are joined. Inline content goes in at the
    // caret. A block fragment splits the paragraph or heading it goes into, its leading inline content ends the first
    // half and its trailing inline content starts the second. The caret is left after what was inserted.

    export class BitRichTextEditorInsert {
        private static readonly BLOCK = /^(ADDRESS|ARTICLE|ASIDE|AUDIO|BLOCKQUOTE|DD|DETAILS|DIV|DL|DT|FIGCAPTION|FIGURE|FOOTER|H[1-6]|HEADER|HR|IFRAME|LI|MAIN|NAV|OL|P|PRE|SECTION|SUMMARY|TABLE|TBODY|TD|TFOOT|TH|THEAD|TR|UL|VIDEO)$/;

        // The blocks split around a block inserted into them, and joined when a deletion goes from one into another.
        private static readonly SPLITS = /^(P|H[1-6]|PRE|DIV|ADDRESS)$/;
        private static readonly JOINS = /^(P|H[1-6]|PRE|DIV|ADDRESS|LI)$/;

        // The blocks with no line of their own to type on: the caret goes on past them.
        private static readonly SOLID = /^(HR|TABLE|IFRAME|VIDEO|AUDIO|FIGURE)$/;

        private static readonly VOID = 'img,hr,iframe,video,audio,table,input,svg';

        // Replaces the range with the markup and returns the caret after it, or null when there is nothing to insert.
        public static insertHtml(root: HTMLElement, range: Range, html: string): Range | null {
            if (!html) return null;
            const tpl = document.createElement('template');
            tpl.innerHTML = html;
            return BitRichTextEditorInsert.insertFragment(root, range, tpl.content);
        }

        // Replaces the range with the text and returns the caret after it. Text typed into a text node goes into that
        // node, as the engines' own typing does, so the input rules that read the text before the caret still find it
        // there; a line break is a <br>.
        public static insertText(root: HTMLElement, range: Range, text: string): Range | null {
            if (!text) return null;
            const at = BitRichTextEditorInsert.clear(root, range);
            const lines = text.replace(/\r\n?/g, '\n').split('\n');
            if (lines.length === 1) {
                const c = at.startContainer;
                if (c.nodeType === 3) {
                    (c as Text).insertData(at.startOffset, text);
                    return BitRichTextEditorInsert.caret(c, at.startOffset + text.length);
                }
                const before = c.childNodes[at.startOffset - 1];
                if (before && before.nodeType === 3) {
                    const length = (before as Text).length;
                    (before as Text).appendData(text);
                    return BitRichTextEditorInsert.caret(before, length + text.length);
                }
                const after = c.childNodes[at.startOffset];
                if (after && after.nodeType === 3) {
                    (after as Text).insertData(0, text);
                    return BitRichTextEditorInsert.caret(after, text.length);
                }
            }
            const fragment = document.createDocumentFragment();
            lines.forEach((line, i) => {
                if (i > 0) fragment.appendChild(document.createElement('br'));
                if (line) fragment.appendChild(document.createTextNode(line));
            });
            return BitRichTextEditorInsert.insertFragment(root, at, fragment);
        }

        public static insertFragment(root: HTMLElement, range: Range, fragment: DocumentFragment): Range | null {
            if (!fragment.firstChild) return null;
            const at = BitRichTextEditorInsert.clear(root, range);
            const nodes = Array.from(fragment.childNodes);
            const isBlock = (n: Node) => n.nodeType === 1 && BitRichTextEditorInsert.BLOCK.test(n.nodeName);
            const first = nodes.findIndex(isBlock);

            // Inline content goes in at the caret, in a paragraph where the caret is in none.
            if (first < 0) {
                if (!BitRichTextEditorInsert.blockOf(root, at.startContainer)) {
                    const p = document.createElement('p');
                    at.insertNode(p);
                    at.setStart(p, 0);
                    at.collapse(true);
                }
                const last = nodes[nodes.length - 1];
                at.insertNode(fragment);
                return BitRichTextEditorInsert.after(last);
            }

            // A block fragment: the inline content before its first block and after its last one joins the halves of
            // the block it splits; inline content between its blocks is a paragraph of its own.
            let last = nodes.length - 1;
            while (!isBlock(nodes[last])) last--;
            const leading = nodes.slice(0, first);
            const trailing = nodes.slice(last + 1);
            const middle: Node[] = [];
            let run: Node[] = [];
            const flush = () => {
                if (run.some(n => n.nodeType === 1 || /\S/.test(n.nodeValue || ''))) {
                    const p = document.createElement('p');
                    p.append(...run);
                    middle.push(p);
                }
                run = [];
            };
            for (const n of nodes.slice(first, last + 1)) {
                if (isBlock(n)) {
                    flush();
                    middle.push(n);
                } else {
                    run.push(n);
                }
            }
            flush();

            const block = BitRichTextEditorInsert.blockOf(root, at.startContainer);
            if (!block || !BitRichTextEditorInsert.SPLITS.test(block.nodeName)) {
                // In a list item, a cell, a quote or the surface itself, the blocks go in where the caret is.
                const tail = trailing[trailing.length - 1] || middle[middle.length - 1];
                const all = document.createDocumentFragment();
                all.append(...leading, ...middle, ...trailing);
                at.insertNode(all);
                return BitRichTextEditorInsert.endOf(tail);
            }

            const tail = BitRichTextEditorInsert.split(block, at);
            block.append(...leading);
            let previous: Node = block;
            for (const m of middle) {
                previous.parentNode!.insertBefore(m, previous.nextSibling);
                previous = m;
            }
            tail.prepend(...trailing);
            BitRichTextEditorInsert.dropEmptyInlines(block);
            BitRichTextEditorInsert.dropEmptyInlines(tail);
            if (!BitRichTextEditorInsert.hasContent(block)) block.remove();

            const lastMiddle = middle[middle.length - 1];
            if (trailing.length > 0) return BitRichTextEditorInsert.after(trailing[trailing.length - 1]);
            if (!BitRichTextEditorInsert.hasContent(tail)) {
                if (!BitRichTextEditorInsert.SOLID.test(lastMiddle.nodeName)) {
                    tail.remove();
                    return BitRichTextEditorInsert.endOf(lastMiddle);
                }
                // A rule or a table at the end of a line leaves an empty paragraph after it to go on typing in.
                const p = document.createElement('p');
                p.appendChild(document.createElement('br'));
                tail.replaceWith(p);
                return BitRichTextEditorInsert.caret(p, 0);
            }
            return BitRichTextEditorInsert.SOLID.test(lastMiddle.nodeName)
                ? BitRichTextEditorInsert.caret(tail, 0)
                : BitRichTextEditorInsert.endOf(lastMiddle);
        }

        // Deletes the range's content, joins the two blocks it went from and to, and takes the placeholder <br> out of
        // a line left empty. Returns the caret where the content was.
        private static clear(root: HTMLElement, range: Range): Range {
            const at = range.cloneRange();
            if (!at.collapsed) {
                const from = BitRichTextEditorInsert.blockOf(root, at.startContainer);
                const to = BitRichTextEditorInsert.blockOf(root, at.endContainer);
                const start = at.startContainer, offset = at.startOffset;
                at.deleteContents();
                // A deletion across blocks leaves the range between them; the caret belongs where the content began,
                // which is still there (the start of a range is never inside what it deletes).
                if (start.isConnected) {
                    at.setStart(start, Math.min(offset, start.nodeType === 3 ? (start as Text).length : start.childNodes.length));
                    at.collapse(true);
                }
                if (from && to && from !== to && from.isConnected && to.isConnected
                    && BitRichTextEditorInsert.JOINS.test(from.nodeName) && BitRichTextEditorInsert.JOINS.test(to.nodeName)) {
                    from.append(...Array.from(to.childNodes));
                    // The block and the lists or quotes the deletion emptied go with it.
                    let gone: Node = to;
                    while (gone.parentNode && gone.parentNode !== root && gone.parentNode.childNodes.length === 1) gone = gone.parentNode;
                    gone.parentNode?.removeChild(gone);
                }
            }

            const c = at.startContainer;
            if (c.nodeType === 1 && c !== root && !BitRichTextEditorInsert.hasContent(c as Element)) {
                Array.from(c.childNodes).forEach(n => { if (n.nodeName === 'BR') n.remove(); });
                at.setStart(c, Math.min(at.startOffset, c.childNodes.length));
                at.collapse(true);
            }
            return at;
        }

        // Splits the block at the caret: what comes after the caret goes into a copy of the block placed after it.
        private static split(block: HTMLElement, at: Range): HTMLElement {
            const rest = document.createRange();
            rest.setStart(at.startContainer, at.startOffset);
            rest.setEnd(block, block.childNodes.length);
            const tail = block.cloneNode(false) as HTMLElement;
            tail.removeAttribute('id');
            tail.appendChild(rest.extractContents());
            block.parentNode!.insertBefore(tail, block.nextSibling);
            return tail;
        }

        private static blockOf(root: Node, node: Node): HTMLElement | null {
            for (let n: Node | null = node; n && n !== root; n = n.parentNode) {
                if (n.nodeType === 1 && BitRichTextEditorInsert.BLOCK.test(n.nodeName)) return n as HTMLElement;
            }
            return null;
        }

        private static hasContent(el: Element): boolean {
            return (el.textContent || '').length > 0 || !!el.querySelector(BitRichTextEditorInsert.VOID);
        }

        // Takes out the inline elements a split left empty (the copies of a mark the caret was at the edge of).
        private static dropEmptyInlines(block: Element) {
            (Array.from(block.querySelectorAll('*')) as Element[]).reverse().forEach(el => {
                if (BitRichTextEditorInsert.BLOCK.test(el.nodeName) || el.nodeName === 'BR' || el.matches(BitRichTextEditorInsert.VOID)) return;
                if (!BitRichTextEditorInsert.hasContent(el) && !el.querySelector('br')) el.remove();
            });
        }

        private static caret(node: Node, offset: number): Range {
            const r = document.createRange();
            r.setStart(node, offset);
            r.collapse(true);
            return r;
        }

        // The caret right after a node: at the end of a text, else behind the node.
        private static after(node: Node): Range {
            if (node.nodeType === 3) return BitRichTextEditorInsert.caret(node, (node as Text).length);
            const r = document.createRange();
            r.setStartAfter(node);
            r.collapse(true);
            return r;
        }

        // The caret at the end of the content of a node: in its last text, else behind its last child.
        private static endOf(node: Node): Range {
            if (node.nodeType === 3 || BitRichTextEditorInsert.SOLID.test(node.nodeName) || node.nodeName === 'IMG') {
                return BitRichTextEditorInsert.after(node);
            }
            for (let n: Node | null = node.lastChild; n; n = n.lastChild) {
                if (n.nodeType === 3) return BitRichTextEditorInsert.caret(n, (n as Text).length);
                if (n.nodeName === 'BR') {
                    const r = document.createRange();
                    r.setStartBefore(n);
                    r.collapse(true);
                    return r;
                }
                if (!n.lastChild) return BitRichTextEditorInsert.after(n);
            }
            return BitRichTextEditorInsert.caret(node, node.childNodes.length);
        }
    }
}
