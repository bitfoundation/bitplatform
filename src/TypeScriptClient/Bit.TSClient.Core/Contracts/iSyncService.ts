module Bit.Contracts {
    export interface ISyncService {
        init(onlineContextFactory: () => Promise<$data.EntityContext>, offlineContextFactory: () => Promise<$data.EntityContext>): void;
        addEntitySetConfig<TEntityContext extends $data.EntityContext>(entitySet: { name: keyof TEntityContext, dtoType: any }, getMethod?: (context: TEntityContext) => $data.Queryable<Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean | (() => boolean), toServerSync?: boolean | (() => boolean) });
        syncContext(): Promise<void>;
        syncEntitySet<TEntityContext extends $data.EntityContext>(entitySetName: keyof TEntityContext): Promise<void>;
        syncEntitySets<TEntityContext extends $data.EntityContext>(entitySetNames: Array<keyof TEntityContext>): Promise<void>;
    }
}