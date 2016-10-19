module Foundation.Model.Contracts {
    export interface IEntityWithDefaultKey<TKey> extends IEntity {
        Id: TKey;
    }
}