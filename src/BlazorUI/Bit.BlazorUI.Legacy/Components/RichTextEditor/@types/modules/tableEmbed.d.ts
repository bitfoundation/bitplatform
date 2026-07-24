type CellData = {
    content?: Delta['ops'];
    attributes?: Record<string, unknown>;
};

type TableRowColumnOp = Omit<Op, 'insert'> & {
    insert?: {
        id: string;
    };
};

interface TableData {
    rows?: Delta['ops'];
    columns?: Delta['ops'];
    cells?: Record<string, CellData>;
}

declare const composePosition: (delta: Delta, index: number) => number | null;

declare const tableHandler: {
    compose(a: TableData, b: TableData, keepNull?: boolean): TableData;
    transform(a: TableData, b: TableData, priority: boolean): TableData;
    invert(change: TableData, base: TableData): TableData;
};

declare class TableEmbed extends Module {
    static register(): void;
}
