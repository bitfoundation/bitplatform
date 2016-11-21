module Foundation.ViewModel.Contracts {
    export interface ISyncService {
        init(onlineContextFactory: () => Promise<$data.EntityContext>, offlineContextFactory: () => Promise<$data.EntityContext>): void;
        addEntitySetConfig<TEntityContext extends $data.EntityContext>(entitySet: $data.EntitySet<Foundation.Model.Contracts.ISyncableDto>, getMethod?: (context: TEntityContext) => $data.Queryable<Foundation.Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean | (() => boolean), toServerSync?: boolean | (() => boolean) });
        syncContext(): Promise<void>;
        syncEntitySet(entitySetName: string): Promise<void>;
        syncEntitySets(entitySetNames: string[]): Promise<void>;
    }
}