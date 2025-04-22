interface EmbedContextRange {
    startNode: Node | Text;
    startOffset: number;
    endNode?: Node | Text;
    endOffset?: number;
}

declare class Embed extends EmbedBlot {
    contentNode: HTMLSpanElement;
    leftGuard: Text;
    rightGuard: Text;
    constructor(scroll: ScrollBlot, node: Node);
    index(node: Node, offset: number): number;
    restore(node: Text): EmbedContextRange | null;
    update(mutations: MutationRecord[], context: Record<string, unknown>): void;
}
