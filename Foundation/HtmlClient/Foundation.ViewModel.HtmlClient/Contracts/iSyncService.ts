module Foundation.ViewModel.Contracts {
    export interface ISyncService {
        init(onlineContextFactory: () => Promise<$data.EntityContext>, offlineContextFactory: () => Promise<$data.EntityContext>): void;
        addEntitySetConfig<TEntityContext extends $data.EntityContext>(entitySet: $data.EntitySet<Model.Contracts.ISyncableDto>, getMethod?: (context: TEntityContext) => $data.Queryable<Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean | (() => boolean), toServerSync?: boolean | (() => boolean) });
        syncContext(): Promise<void>;
        syncEntitySet<TEntityContext extends $data.EntityContext>(entitySetName: keyof TEntityContext): Promise<void>;
        syncEntitySets<TEntityContext extends $data.EntityContext>(entitySetNames: Array<keyof TEntityContext>): Promise<void>;
    }
}