interface Array<T> {
    toQueryable(entityType): $data.Queryable<$data.Entity>;
    totalCount: number;
}

declare class Decimal {
    public constructor(value: string) { };
    static add(firstValue: string, secondValue: string): string;
    static mul(firstValue: string, secondValue: string): string;
    static div(firstValue: string, secondValue: string): string;
    static sub(firstValue: string, secondValue: string): string;
    toFixed(size: number): string;
}

declare var clientAppProfile: Bit.Contracts.IClientAppProfile;
declare var odatajs: any;