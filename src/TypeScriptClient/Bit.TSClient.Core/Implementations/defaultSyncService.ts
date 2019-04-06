module Bit.Implementations {

    export class DefaultSyncService implements Contracts.ISyncService {

        private onlineContextFactory: () => Promise<$data.EntityContext>;
        private offlineContextFactory: () => Promise<$data.EntityContext>;

        @Log()
        public init(onlineContextFactory: () => Promise<$data.EntityContext>, offlineContextFactory: () => Promise<$data.EntityContext>): void {
            if (onlineContextFactory == null) {
                throw new Error("onlineContextFactory may not be null");
            }
            if (offlineContextFactory == null) {
                throw new Error("offlineContextFactory may not be null");
            }
            this.offlineContextFactory = offlineContextFactory;
            this.onlineContextFactory = onlineContextFactory;
        }

        private entitySetConfigs: Array<{ entitySetName: string, keyMembers: string[], getMethod?: (context: $data.EntityContext) => $data.Queryable<Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean | (() => boolean), toServerSync?: boolean | (() => boolean) } }> = [];

        @Log()
        public addEntitySetConfig<TEntityContext extends $data.EntityContext>(entitySet: { name: keyof TEntityContext & string, dtoType: any }, getMethod?: (context: TEntityContext) => $data.Queryable<Model.Contracts.ISyncableDto>, syncConfig?: { fromServerSync?: boolean | (() => boolean), toServerSync?: boolean | (() => boolean) }) {

            if (entitySet == null) {
                throw new Error("entitySet may not be null");
            }
            if (entitySet.dtoType == null) {
                throw new Error("entitySet's dto type may not be null");
            }
            if (entitySet.name == null) {
                throw new Error("entitySet's name may not be null");
            }

            if (getMethod == null) {
                getMethod = (cntx) => cntx[entitySet.name] as any;
            }

            if (syncConfig == null) {
                syncConfig = { fromServerSync: true, toServerSync: true };
            }
            if (syncConfig.fromServerSync == null) {
                syncConfig.fromServerSync = true;
            }
            if (syncConfig.toServerSync == null) {
                syncConfig.toServerSync = true;
            }

            let keyMembers = entitySet.dtoType.memberDefinitions.getKeyProperties().map(function (k) { return k.name; });

            let entitySetConfig = {
                entitySetName: entitySet.name,
                getMethod: getMethod,
                syncConfig: syncConfig,
                keyMembers: keyMembers
            };

            this.entitySetConfigs.push(entitySetConfig);
        }

        @Log()
        public async syncContext(): Promise<void> {
            if (typeof (performance) != "undefined") {
                performance.mark("bit-sync-start");
            }
            try {
                await this.syncEntitySets(this.entitySetConfigs.map(entitySetConfig => { return entitySetConfig.entitySetName as any; }));
            }
            finally {
                if (typeof (performance) != "undefined") {
                    performance.mark("bit-sync-finish");
                    performance.measure("bit-sync", "bit-sync-start", "bit-sync-finish");
                }
            }
        }

        @Log()
        public async syncEntitySet<TEntityContext extends $data.EntityContext>(entitySetName: keyof TEntityContext & string): Promise<void> {
            if (typeof (performance) != "undefined") {
                performance.mark(`bit-sync-${entitySetName}-start`);
            }
            try {
                await this.syncEntitySets<TEntityContext>([entitySetName]);
            }
            finally {
                if (typeof (performance) != "undefined") {
                    performance.mark(`bit-sync-${entitySetName}-finish`);
                    performance.measure(`bit-sync-${entitySetName}`, `bit-sync-${entitySetName}-start`, `bit-sync-${entitySetName}-finish`);
                }
            }
        }

        public async syncEntitySets<TEntityContext extends $data.EntityContext>(entitySetNames: Array<keyof TEntityContext & string>): Promise<void> {

            if (entitySetNames == null) {
                throw new Error("entitySetNames may not be null");
            }

            if (entitySetNames.length == 0) {
                throw new Error("entitySetNames may not be empty");
            }

            if (entitySetNames.some(entitySetName => entitySetName == null)) {
                throw new Error("entitySetName may not be null");
            }

            const entitySetConfigs = entitySetNames
                .map(entitySetName => {
                    const cnfg = this.entitySetConfigs.find(ec => ec.entitySetName.toLowerCase() == entitySetName.toLowerCase());
                    if (cnfg == null) {
                        throw new Error(`No entity set config found named ${entitySetName}`);
                    }
                    return cnfg;
                });

            if (navigator.onLine == false) {
                return;
            }

            const onlineContext = await this.onlineContextFactory();
            const offlineContext = await this.offlineContextFactory();
            offlineContext["ignoreEntityEvents"] = true;

            const entitySetSyncMaterials = entitySetConfigs.map(entitySetConfig => {
                return {
                    entitySetConfig: entitySetConfig,
                    offlineEntitySet: offlineContext[entitySetConfig.entitySetName] as $data.EntitySet<Model.Contracts.ISyncableDto>,
                    onlineEntitySet: onlineContext[entitySetConfig.entitySetName] as $data.EntitySet<Model.Contracts.ISyncableDto>
                };
            });

            const toServerEntitySetSyncMaterials = entitySetSyncMaterials.filter(entitySetSyncMaterial => entitySetSyncMaterial.entitySetConfig.syncConfig.toServerSync == true || (typeof entitySetSyncMaterial.entitySetConfig.syncConfig.toServerSync == "function" && entitySetSyncMaterial.entitySetConfig.syncConfig.toServerSync() == true));
            const fromServerEntitySetSyncMaterials = entitySetSyncMaterials.filter(entitySetSyncMaterial => entitySetSyncMaterial.entitySetConfig.syncConfig.fromServerSync == true || (typeof entitySetSyncMaterial.entitySetConfig.syncConfig.fromServerSync == "function" && entitySetSyncMaterial.entitySetConfig.syncConfig.fromServerSync() == true));

            if (toServerEntitySetSyncMaterials.length > 0) {

                const allRecentlyChangedOfflineEntities = await offlineContext.batchExecuteQuery(toServerEntitySetSyncMaterials.map(entitySetSyncMaterial => {
                    return entitySetSyncMaterial.offlineEntitySet.filter(e => e.IsSynced == false);
                }));

                for (let i = 0; i < toServerEntitySetSyncMaterials.length; i++) {

                    const entitySetSyncMaterial = toServerEntitySetSyncMaterials[i];
                    const recentlyChangedOfflineEntities = allRecentlyChangedOfflineEntities[i];

                    for (const modifiedOfflineEntity of recentlyChangedOfflineEntities) {
                        const clonedEntityToBeSavedInOnlineContext = entitySetSyncMaterial.onlineEntitySet.elementType["create"](modifiedOfflineEntity["initData"]) as Model.Contracts.ISyncableDto;
                        onlineContext.attach(clonedEntityToBeSavedInOnlineContext, $data.EntityAttachMode.AllChanged);
                        if (clonedEntityToBeSavedInOnlineContext.IsArchived == true) {
                            clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Deleted;
                        } else if (clonedEntityToBeSavedInOnlineContext.Version == null || clonedEntityToBeSavedInOnlineContext.Version == "0000000000000000000") {
                            clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Added;
                        } else {
                            clonedEntityToBeSavedInOnlineContext.entityState = $data.EntityState.Modified;
                        }
                    }
                }

                await onlineContext.saveChanges();
            }

            if (fromServerEntitySetSyncMaterials.length > 0) {

                const allOfflineEntitiesOrderedByVersion = await offlineContext.batchExecuteQuery(fromServerEntitySetSyncMaterials.map(entitySetSyncMaterial => {
                    return entitySetSyncMaterial.offlineEntitySet.orderByDescending(e => e.Version).map(function (it) { return { Version: it.Version }; }).take(1);
                })) as Model.Contracts.ISyncableDto[][];

                const loadRecentlyChangedOnlineEntitiesQueries = fromServerEntitySetSyncMaterials.map((entitySetSyncMaterial, i) => {

                    const offlineEntitiesOrderedByVersion = allOfflineEntitiesOrderedByVersion[i];

                    let maxVersion = "0000000000000000000";

                    if (offlineEntitiesOrderedByVersion[0] != null) {
                        maxVersion = offlineEntitiesOrderedByVersion[0].Version;
                    }

                    const recentlyChangedOnlineEntitiesQuery = entitySetSyncMaterial.entitySetConfig.getMethod(onlineContext).filter((e, ver) => e.Version > ver, { ver: maxVersion });

                    return recentlyChangedOnlineEntitiesQuery;

                });

                const allRecentlyChangedOnlineEntities = (await onlineContext.batchExecuteQuery(loadRecentlyChangedOnlineEntitiesQueries)) as Model.Contracts.ISyncableDto[][];

                for (let i = 0; i < allRecentlyChangedOnlineEntities.length; i++) {

                    const recentlyChangedOnlineEntities = allRecentlyChangedOnlineEntities[i];
                    if (recentlyChangedOnlineEntities.length == 0) {
                        continue;
                    }
                    const offlineEntitiesOrderedByVersion = allOfflineEntitiesOrderedByVersion[i];
                    const entitySetSyncMaterial = fromServerEntitySetSyncMaterials[i];

                    if (offlineEntitiesOrderedByVersion.length == 0) {
                        for (let onlineEntity of recentlyChangedOnlineEntities) {
                            onlineEntity.IsSynced = true;
                            onlineEntity.Version = onlineEntity.Version.padStart(19, "0");
                        }
                        entitySetSyncMaterial.offlineEntitySet.addMany(recentlyChangedOnlineEntities);
                    } else {

                        const mapString = `function(it) { return { ${entitySetSyncMaterial.entitySetConfig.keyMembers.map(function (k) { return `${k}: it.${k}`; }).join(",")} } }`;

                        let recentlyChangedOnlineEntitiesCopy = recentlyChangedOnlineEntities.slice();
                        let maxParametersCount = offlineContext["storageProvider"].name == "webSql" ? 127 : recentlyChangedOnlineEntitiesCopy.length;

                        let equivalentOfflineEntities = [];

                        while (recentlyChangedOnlineEntitiesCopy.length != 0) {
                            for (const keyMember of entitySetSyncMaterial.entitySetConfig.keyMembers) {
                                equivalentOfflineEntities = equivalentOfflineEntities.concat(await entitySetSyncMaterial.offlineEntitySet.map(mapString).filter(`function (it, keys) { return it.${keyMember} in keys; }`, { keys: recentlyChangedOnlineEntitiesCopy.splice(0, maxParametersCount).map(e => { return e[keyMember]; }) }).toArray());
                            }
                        }

                        for (const recentlyChangedOnlineEntity of recentlyChangedOnlineEntities) {

                            const hasEquivalentInOfflineContext = equivalentOfflineEntities.some(e => entitySetSyncMaterial.entitySetConfig.keyMembers.every(key => e[key] == recentlyChangedOnlineEntity[key]));

                            if (recentlyChangedOnlineEntity.IsArchived == false || hasEquivalentInOfflineContext == true) {

                                const clonedEntity = entitySetSyncMaterial.offlineEntitySet.elementType["create"](recentlyChangedOnlineEntity["initData"]) as Model.Contracts.ISyncableDto;
                                clonedEntity.IsSynced = true;
                                clonedEntity.Version = clonedEntity.Version.padStart(19, "0");
                                offlineContext.attach(clonedEntity, $data.EntityAttachMode.AllChanged);

                                if (clonedEntity.IsArchived == true) {
                                    clonedEntity.entityState = $data.EntityState.Deleted;
                                } else if (hasEquivalentInOfflineContext == true) {
                                    clonedEntity.entityState = $data.EntityState.Modified;
                                } else if (hasEquivalentInOfflineContext == false) {
                                    clonedEntity.entityState = $data.EntityState.Added;
                                }
                            }
                        }
                    }

                }

                await offlineContext.saveChanges();

            }
        }
    }
}