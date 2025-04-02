declare class Attributor {
    readonly attrName: string;
    readonly keyName: string;
    static keys(node: HTMLElement): string[];
    scope: Scope;
    whitelist: string[] | undefined;
    constructor(attrName: string, keyName: string, options?: AttributorOptions);
    add(node: HTMLElement, value: any): boolean;
    canAdd(_node: HTMLElement, value: any): boolean;
    remove(node: HTMLElement): void;
    value(node: HTMLElement): any;
}

declare interface AttributorOptions {
    scope?: Scope;
    whitelist?: string[];
}

declare class AttributorStore {
    private attributes;
    private domNode;
    constructor(domNode: HTMLElement);
    attribute(attribute: Attributor, value: any): void;
    build(): void;
    copy(target: Formattable): void;
    move(target: Formattable): void;
    values(): {
        [key: string]: any;
    };
}

declare class BlockBlot extends ParentBlot implements Formattable {
    static blotName: string;
    static scope: Scope;
    static tagName: string | string[];
    static allowedChildren: BlotConstructor[];
    static create(value?: unknown): HTMLElement;
    static formats(domNode: HTMLElement, scroll: Root): any;
    protected attributes: AttributorStore;
    constructor(scroll: Root, domNode: Node);
    format(name: string, value: any): void;
    formats(): {
        [index: string]: any;
    };
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    replaceWith(name: string | Blot, value?: any): Blot;
    update(mutations: MutationRecord[], context: {
        [key: string]: any;
    }): void;
}

/**
 * Blots are the basic building blocks of a Parchment document.
 *
 * Several basic implementations such as Block, Inline, and Embed are provided.
 * In general you will want to extend one of these, instead of building from scratch.
 * After implementation, blots need to be registered before usage.
 *
 * At the very minimum a Blot must be named with a static blotName and associated with either a tagName or className.
 * If a Blot is defined with both a tag and class, the class takes precedence, but the tag may be used as a fallback.
 * Blots must also have a scope, which determine if it is inline or block.
 */
declare interface Blot extends LinkedNode {
    scroll: Root;
    parent: Parent;
    prev: Blot | null;
    next: Blot | null;
    domNode: Node;
    statics: BlotConstructor;
    attach(): void;
    clone(): Blot;
    detach(): void;
    isolate(index: number, length: number): Blot;
    /**
     * For leaves, length of blot's value()
     * For parents, sum of children's values
     */
    length(): number;
    /**
     * Returns offset between this blot and an ancestor's
     */
    offset(root?: Blot): number;
    remove(): void;
    replaceWith(name: string, value: any): Blot;
    replaceWith(replacement: Blot): Blot;
    split(index: number, force?: boolean): Blot | null;
    wrap(name: string, value?: any): Parent;
    wrap(wrapper: Parent): Parent;
    deleteAt(index: number, length: number): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    /**
     * Called after update cycle completes. Cannot change the value or length
     * of the document, and any DOM operation must reduce complexity of the DOM
     * tree. A shared context object is passed through all blots.
     */
    optimize(context: {
        [key: string]: any;
    }): void;
    optimize(mutations: MutationRecord[], context: {
        [key: string]: any;
    }): void;
    /**
     * Called when blot changes, with the mutation records of its change.
     * Internal records of the blot values can be updated, and modifications of
     * the blot itself is permitted. Can be trigger from user change or API call.
     * A shared context object is passed through all blots.
     */
    update(mutations: MutationRecord[], context: {
        [key: string]: any;
    }): void;
}

declare interface BlotConstructor {
    new (...args: any[]): Blot;
    /**
     * Creates corresponding DOM node
     */
    create(value?: any): Node;
    blotName: string;
    tagName: string | string[];
    scope: Scope;
    className?: string;
    requiredContainer?: BlotConstructor;
    allowedChildren?: BlotConstructor[];
    defaultChild?: BlotConstructor;
}

declare class ClassAttributor extends Attributor {
    static keys(node: HTMLElement): string[];
    add(node: HTMLElement, value: any): boolean;
    remove(node: HTMLElement): void;
    value(node: HTMLElement): any;
}

declare class ContainerBlot extends ParentBlot {
    static blotName: string;
    static scope: Scope;
    static tagName: string | string[];
    prev: BlockBlot | ContainerBlot | null;
    next: BlockBlot | ContainerBlot | null;
    checkMerge(): boolean;
    deleteAt(index: number, length: number): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    optimize(context: {
        [key: string]: any;
    }): void;
}

declare class EmbedBlot extends LeafBlot implements Formattable {
    static formats(_domNode: HTMLElement, _scroll: Root): any;
    format(name: string, value: any): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    formats(): {
        [index: string]: any;
    };
}

declare interface Formattable extends Blot {
    /**
     * Apply format to blot. Should not pass onto child or other blot.
     */
    format(name: string, value: any): void;
    /**
     * Return formats represented by blot, including from Attributors.
     */
    formats(): {
        [index: string]: any;
    };
}

declare class InlineBlot extends ParentBlot implements Formattable {
    static allowedChildren: BlotConstructor[];
    static blotName: string;
    static scope: Scope;
    static tagName: string | string[];
    static create(value?: unknown): HTMLElement;
    static formats(domNode: HTMLElement, scroll: Root): any;
    protected attributes: AttributorStore;
    constructor(scroll: Root, domNode: Node);
    format(name: string, value: any): void;
    formats(): {
        [index: string]: any;
    };
    formatAt(index: number, length: number, name: string, value: any): void;
    optimize(context: {
        [key: string]: any;
    }): void;
    replaceWith(name: string | Blot, value?: any): Blot;
    update(mutations: MutationRecord[], context: {
        [key: string]: any;
    }): void;
    wrap(name: string | Parent, value?: any): Parent;
}

declare interface Leaf extends Blot {
    index(node: Node, offset: number): number;
    position(index: number, inclusive: boolean): [Node, number];
    value(): any;
}

declare class LeafBlot extends ShadowBlot implements Leaf {
    static scope: Scope;
    /**
     * Returns the value represented by domNode if it is this Blot's type
     * No checking that domNode can represent this Blot type is required so
     * applications needing it should check externally before calling.
     */
    static value(_domNode: Node): any;
    /**
     * Given location represented by node and offset from DOM Selection Range,
     * return index to that location.
     */
    index(node: Node, offset: number): number;
    /**
     * Given index to location within blot, return node and offset representing
     * that location, consumable by DOM Selection Range
     */
    position(index: number, _inclusive?: boolean): [Node, number];
    /**
     * Return value represented by this blot
     * Should not change without interaction from API or
     * user change detectable by update()
     */
    value(): any;
}

declare class LinkedList<T extends LinkedNode> {
    head: T | null;
    tail: T | null;
    length: number;
    constructor();
    append(...nodes: T[]): void;
    at(index: number): T | null;
    contains(node: T): boolean;
    indexOf(node: T): number;
    insertBefore(node: T | null, refNode: T | null): void;
    offset(target: T): number;
    remove(node: T): void;
    iterator(curNode?: T | null): () => T | null;
    find(index: number, inclusive?: boolean): [T | null, number];
    forEach(callback: (cur: T) => void): void;
    forEachAt(index: number, length: number, callback: (cur: T, offset: number, length: number) => void): void;
    map(callback: (cur: T) => any): any[];
    reduce<M>(callback: (memo: M, cur: T) => M, memo: M): M;
}

declare interface LinkedNode {
    prev: LinkedNode | null;
    next: LinkedNode | null;
    length(): number;
}

declare interface Parent extends Blot {
    children: LinkedList<Blot>;
    domNode: HTMLElement;
    appendChild(child: Blot): void;
    descendant<T>(type: new () => T, index: number): [T, number];
    descendant<T>(matcher: (blot: Blot) => boolean, index: number): [T, number];
    descendants<T>(type: new () => T, index: number, length: number): T[];
    descendants<T>(matcher: (blot: Blot) => boolean, index: number, length: number): T[];
    insertBefore(child: Blot, refNode?: Blot | null): void;
    moveChildren(parent: Parent, refNode?: Blot | null): void;
    path(index: number, inclusive?: boolean): [Blot, number][];
    removeChild(child: Blot): void;
    unwrap(): void;
}

declare class ParentBlot extends ShadowBlot implements Parent {
    /**
     * Whitelist array of Blots that can be direct children.
     */
    static allowedChildren?: BlotConstructor[];
    /**
     * Default child blot to be inserted if this blot becomes empty.
     */
    static defaultChild?: BlotConstructor;
    static uiClass: string;
    children: LinkedList<Blot>;
    domNode: HTMLElement;
    uiNode: HTMLElement | null;
    constructor(scroll: Root, domNode: Node);
    appendChild(other: Blot): void;
    attach(): void;
    attachUI(node: HTMLElement): void;
    /**
     * Called during construction, should fill its own children LinkedList.
     */
    build(): void;
    deleteAt(index: number, length: number): void;
    descendant<T extends Blot>(criteria: new (...args: any[]) => T, index: number): [T | null, number];
    descendant(criteria: (blot: Blot) => boolean, index: number): [Blot | null, number];
    descendants<T extends Blot>(criteria: new (...args: any[]) => T, index?: number, length?: number): T[];
    descendants(criteria: (blot: Blot) => boolean, index?: number, length?: number): Blot[];
    detach(): void;
    enforceAllowedChildren(): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    insertBefore(childBlot: Blot, refBlot?: Blot | null): void;
    length(): number;
    moveChildren(targetParent: Parent, refNode?: Blot | null): void;
    optimize(context?: {
        [key: string]: any;
    }): void;
    path(index: number, inclusive?: boolean): [Blot, number][];
    removeChild(child: Blot): void;
    replaceWith(name: string | Blot, value?: any): Blot;
    split(index: number, force?: boolean): Blot | null;
    splitAfter(child: Blot): Parent;
    unwrap(): void;
    update(mutations: MutationRecord[], _context: {
        [key: string]: any;
    }): void;
}

declare class Registry implements RegistryInterface {
    static blots: WeakMap<Node, Blot>;
    static find(node?: Node | null, bubble?: boolean): Blot | null;
    private attributes;
    private classes;
    private tags;
    private types;
    create(scroll: Root, input: Node | string | Scope, value?: any): Blot;
    find(node: Node | null, bubble?: boolean): Blot | null;
    query(query: string | Node | Scope, scope?: Scope): RegistryDefinition | null;
    register(...definitions: RegistryDefinition[]): RegistryDefinition[];
}

declare type RegistryDefinition = Attributor | BlotConstructor;

declare interface RegistryInterface {
    create(scroll: Root, input: Node | string | Scope, value?: any): Blot;
    query(query: string | Node | Scope, scope: Scope): RegistryDefinition | null;
    register(...definitions: any[]): any;
}

declare interface Root extends Parent {
    create(input: Node | string | Scope, value?: any): Blot;
    find(node: Node | null, bubble?: boolean): Blot | null;
    query(query: string | Node | Scope, scope?: Scope): RegistryDefinition | null;
}

declare enum Scope {
    TYPE = 3,// 0011 Lower two bits
    LEVEL = 12,// 1100 Higher two bits
    ATTRIBUTE = 13,// 1101
    BLOT = 14,// 1110
    INLINE = 7,// 0111
    BLOCK = 11,// 1011
    BLOCK_BLOT = 10,// 1010
    INLINE_BLOT = 6,// 0110
    BLOCK_ATTRIBUTE = 9,// 1001
    INLINE_ATTRIBUTE = 5,// 0101
    ANY = 15
}

declare class ScrollBlot extends ParentBlot implements Root {
    registry: Registry;
    static blotName: string;
    static defaultChild: typeof BlockBlot;
    static allowedChildren: BlotConstructor[];
    static scope: Scope;
    static tagName: string;
    observer: MutationObserver;
    constructor(registry: Registry, node: HTMLDivElement);
    create(input: Node | string | Scope, value?: any): Blot;
    find(node: Node | null, bubble?: boolean): Blot | null;
    query(query: string | Node | Scope, scope?: Scope): RegistryDefinition | null;
    register(...definitions: RegistryDefinition[]): RegistryDefinition[];
    build(): void;
    detach(): void;
    deleteAt(index: number, length: number): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    optimize(context?: {
        [key: string]: any;
    }): void;
    optimize(mutations: MutationRecord[], context: {
        [key: string]: any;
    }): void;
    update(mutations?: MutationRecord[], context?: {
        [key: string]: any;
    }): void;
}

declare class ShadowBlot implements Blot {
    scroll: Root;
    domNode: Node;
    static blotName: string;
    static className: string;
    static requiredContainer: BlotConstructor;
    static scope: Scope;
    static tagName: string | string[];
    static create(rawValue?: unknown): Node;
    prev: Blot | null;
    next: Blot | null;
    parent: Parent;
    get statics(): any;
    constructor(scroll: Root, domNode: Node);
    attach(): void;
    clone(): Blot;
    detach(): void;
    deleteAt(index: number, length: number): void;
    formatAt(index: number, length: number, name: string, value: any): void;
    insertAt(index: number, value: string, def?: any): void;
    isolate(index: number, length: number): Blot;
    length(): number;
    offset(root?: Blot): number;
    optimize(_context?: {
        [key: string]: any;
    }): void;
    remove(): void;
    replaceWith(name: string | Blot, value?: any): Blot;
    split(index: number, _force?: boolean): Blot | null;
    update(_mutations: MutationRecord[], _context: {
        [key: string]: any;
    }): void;
    wrap(name: string | Parent, value?: any): Parent;
}

declare class StyleAttributor extends Attributor {
    static keys(node: HTMLElement): string[];
    add(node: HTMLElement, value: any): boolean;
    remove(node: HTMLElement): void;
    value(node: HTMLElement): any;
}

declare class TextBlot extends LeafBlot implements Leaf {
    static readonly blotName = "text";
    static scope: Scope;
    static create(value: string): Text;
    static value(domNode: Text): string;
    domNode: Text;
    protected text: string;
    constructor(scroll: Root, node: Node);
    deleteAt(index: number, length: number): void;
    index(node: Node, offset: number): number;
    insertAt(index: number, value: string, def?: any): void;
    length(): number;
    optimize(context: {
        [key: string]: any;
    }): void;
    position(index: number, _inclusive?: boolean): [Node, number];
    split(index: number, force?: boolean): Blot | null;
    update(mutations: MutationRecord[], _context: {
        [key: string]: any;
    }): void;
    value(): string;
}
