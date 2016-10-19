module Foundation.ViewModel.Implementations {

    export class DefaultSyncService implements Foundation.ViewModel.Contracts.ISyncService {

        private onlineContext: $data.EntityContext;
        private offlineContext: $data.EntityContext;
        private onlineContextFactory: () => Promise<$data.EntityContext>;
        private offlineContextFactory: () => Promise<$data.EntityContext>;

        @Foundation.Core.Log()
        public init(onlineContextFactory: () => Promise<$data.EntityContext>, offlineContextFactory: () => Promise<$data.EntityContext>): void {
            if (onlineContextFactory == null)
                throw new Error("onlineContextFactory may not be null");
            if (offlineContextFactory == null)
                throw new Error("offlineContextFactory may not be null");
            this.offlineContextFactory = offlineContextFactory;
            this.onlineContextFactory = onlineContextFactory;
        }

        private entitySetConfigs: Array<{ entitySetName: string, getMethod?: (context: $data.EntityContext) => $data.Queryable<Foundation.Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean, toServerSync?: boolean } }> = [];

        @Foundation.Core.Log()
        public addEntitySetConfig<TEntityContext extends $data.EntityContext>(entitySet: $data.EntitySet<Foundation.Model.Contracts.ISyncableDto>, getMethod?: (context: TEntityContext) => $data.Queryable<Foundation.Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean, toServerSync?: boolean }) {

            if (entitySet == null)
                throw new Error("entitySetName may not be null");

            if (getMethod == null)
                getMethod = (cntx) => cntx[entitySet.collectionName];

            if (syncConfig == null)
                syncConfig = { fromServerSync: true, toServerSync: true };
            if (syncConfig.fromServerSync == null)
                syncConfig.fromServerSync = true;
            if (syncConfig.toServerSync == null)
                syncConfig.toServerSync = true;

            this.entitySetConfigs.push({ entitySetName: entitySet.collectionName, getMethod: getMethod, syncConfig: syncConfig });
        }

        @Foundation.Core.Log()
        public async syncContext(): Promise<void> {
            for (let entitySetConfig of this.entitySetConfigs) {
                await this.syncEntitySet(entitySetConfig.entitySetName);
            }
        }

        @Foundation.Core.Log()
        public async syncEntitySet(entitySetName: string): Promise<void> {

            if (entitySetName == null)
                throw new Error("entitySetName may not be null");

            let entitySetConfig = this.entitySetConfigs.find(ec => ec.entitySetName.toLowerCase() == entitySetName.toLowerCase());

            if (entitySetConfig == null)
                throw new Error("No entity set config found named: " + entitySetName);

            if (navigator.onLine == false)
                return;

            this.onlineContext = await this.onlineContextFactory();
            this.offlineContext = await this.offlineContextFactory();
            this.offlineContext['ignoreEntityEvents'] = true;

            let offlineEntitySet = this.offlineContext[entitySetName] as $data.EntitySet<Foundation.Model.Contracts.ISyncableDto>;
            let onlineEntitySet = this.onlineContext[entitySetName] as $data.EntitySet<Foundation.Model.Contracts.ISyncableDto>;

            let modifiedOfflineEntities = await offlineEntitySet.filter(e => e.ISV != true).toArray();

            if (entitySetConfig.syncConfig.toServerSync == true) {

                for (let modifiedOfflineEntity of modifiedOfflineEntities) {
                    let clonedEntityToBeSavedInOnlineContext = onlineEntitySet.elementType['create'](modifiedOfflineEntity['initData']) as Foundation.Model.Contracts.ISyncableDto;
                    this.onlineContext.attach(clonedEntityToBeSavedInOnlineContext, $data.EntityAttachMode.AllChanged);
                    if (clonedEntityToBeSavedInOnlineContext.IsArchived == true)
                        clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Deleted;
                    else if (clonedEntityToBeSavedInOnlineContext.Version == null || clonedEntityToBeSavedInOnlineContext.Version == "0")
                        clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Added;
                    else
                        clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Modified;
                }

                await this.onlineContext.saveChanges();
            }

            if (entitySetConfig.syncConfig.fromServerSync == true) {

                let offlineEntites = (await offlineEntitySet.map(e => { return { Id: e.Id, Version: e.Version }; })
                    .orderByDescending(e => e.Version)
                    .toArray()) as Foundation.Model.Contracts.ISyncableDto[];

                let maxVersion = "0";
                if (offlineEntites[0] != null)
                    maxVersion = offlineEntites[0].Version;

                let baseQuery = entitySetConfig.getMethod(this.onlineContext).filter((e, ver) => e.Version > ver, { ver: maxVersion });

                let recentlyChangedOnlineEntities = await baseQuery.take(100).toArray();

                for (let recentlyChangedOnlineEntity of recentlyChangedOnlineEntities) {

                    let clonedEntity = offlineEntitySet.elementType['create'](recentlyChangedOnlineEntity['initData']) as Foundation.Model.Contracts.ISyncableDto;

                    this.offlineContext.attach(clonedEntity, $data.EntityAttachMode.AllChanged);

                    if (offlineEntites.some(e => e.Id == clonedEntity.Id)) {
                        if (clonedEntity.IsArchived == true) {
                            clonedEntity.entityState = $data.EntityState.Deleted;
                        }
                        else {
                            clonedEntity.entityState = $data.EntityState.Modified;
                        }
                    }
                    else
                        clonedEntity.entityState = $data.EntityState.Added;
                }

                await this.offlineContext.saveChanges();

            }
        }
    }
}