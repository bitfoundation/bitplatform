declare type EmbedHandler<T> = {
    compose(a: T, b: T, keepNull: boolean): T;
    invert(a: T, b: T): T;
    transform(a: T, b: T, priority: boolean): T;
}

declare class Delta {
    static Op: typeof Op;
    static OpIterator: typeof OpIterator;
    static AttributeMap: typeof AttributeMap;
    private static handlers;
    static registerEmbed<T>(embedType: string, handler: EmbedHandler<T>): void;
    static unregisterEmbed(embedType: string): void;
    private static getHandler;
    ops: Op[];
    constructor(ops?: Op[] | {
        ops: Op[];
    });
    insert(arg: string | Record<string, unknown>, attributes?: AttributeMap | null): this;
    delete(length: number): this;
    retain(length: number | Record<string, unknown>, attributes?: AttributeMap | null): this;
    push(newOp: Op): this;
    chop(): this;
    filter(predicate: (op: Op, index: number) => boolean): Op[];
    forEach(predicate: (op: Op, index: number) => void): void;
    map<T>(predicate: (op: Op, index: number) => T): T[];
    partition(predicate: (op: Op) => boolean): [Op[], Op[]];
    reduce<T>(predicate: (accum: T, curr: Op, index: number) => T, initialValue: T): T;
    changeLength(): number;
    length(): number;
    slice(start?: number, end?: number): Delta;
    compose(other: Delta): Delta;
    concat(other: Delta): Delta;
    diff(other: Delta, cursor?: number | any): Delta;
    eachLine(predicate: (line: Delta, attributes: AttributeMap, index: number) => boolean | void, newline?: string): void;
    invert(base: Delta): Delta;
    transform(index: number, priority?: boolean): number;
    transform(other: Delta, priority?: boolean): Delta;
    transformPosition(index: number, priority?: boolean): number;
}
