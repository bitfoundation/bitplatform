declare type Op = {
    insert?: string | Record<string, unknown>;
    delete?: number;
    retain?: number | Record<string, unknown>;
    attributes?: AttributeMap;
}
declare namespace Op {
    function length(op: Op): number;
}