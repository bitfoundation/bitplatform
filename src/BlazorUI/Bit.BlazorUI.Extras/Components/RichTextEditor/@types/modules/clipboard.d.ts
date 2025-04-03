type Selector = string | Node['TEXT_NODE'] | Node['ELEMENT_NODE'];
type Matcher = (node: Node, delta: Delta, scroll: ScrollBlot) => Delta;
interface ClipboardOptions {
    matchers: [Selector, Matcher][];
}
declare class QuillClipboard extends Module<ClipboardOptions> {
    static DEFAULTS: ClipboardOptions;
    matchers: [Selector, Matcher][];
    constructor(quill: Quill, options: Partial<ClipboardOptions>);
    addMatcher(selector: Selector, matcher: Matcher): void;
    convert({ html, text }: {
        html?: string;
        text?: string;
    }, formats?: Record<string, unknown>): Delta;
    protected normalizeHTML(doc: Document): void;
    protected convertHTML(html: string): Delta;
    dangerouslyPasteHTML(html: string, source?: EmitterSource): void;
    dangerouslyPasteHTML(index: number, html: string, source?: EmitterSource): void;
    onCaptureCopy(e: ClipboardEvent, isCut?: boolean): void;
    private normalizeURIList;
    onCapturePaste(e: ClipboardEvent): void;
    onCopy(range: QuillRange, isCut: boolean): {
        html: string;
        text: string;
    };
    onPaste(range: QuillRange, { text, html }: {
        text?: string;
        html?: string;
    }): void;
    prepareMatching(container: Element, nodeMatches: WeakMap<Node, Matcher[]>): Matcher[][];
}
declare function traverse(scroll: ScrollBlot, node: ChildNode, elementMatchers: Matcher[], textMatchers: Matcher[], nodeMatches: WeakMap<Node, Matcher[]>): Delta;
declare function matchAttributor(node: HTMLElement, delta: Delta, scroll: ScrollBlot): Delta;
declare function matchBlot(node: Node, delta: Delta, scroll: ScrollBlot): Delta;
declare function matchNewline(node: Node, delta: Delta, scroll: ScrollBlot): Delta;
declare function matchText(node: HTMLElement, delta: Delta, scroll: ScrollBlot): Delta;
