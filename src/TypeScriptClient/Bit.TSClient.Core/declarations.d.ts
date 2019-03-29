interface Array<T> {
    toQueryable(entityType): $data.Queryable<$data.Entity>;
    totalCount: number;
}

declare namespace decimal {
    interface IDecimalStatic extends IDecimalConfig {
        add(firstValue: string, secondValue: string): string;
        mul(firstValue: string, secondValue: string): string;
        div(firstValue: string, secondValue: string): string;
        sub(firstValue: string, secondValue: string): string;
    }
}

declare var clientAppProfile: Bit.Contracts.IClientAppProfile;
declare var odatajs: any;