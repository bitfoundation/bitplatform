let testInsert = async (): Promise<void> => {
    const contextProvider = Bit.DependencyManager.getCurrent().resolveObject<Bit.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const model = context.testModels.add({ StringProperty: "Test", DateProperty: new Date() });
    await context.saveChanges();

    if (model.Id == "0") {
        throw new Error("Id may not be zero");
    };
};

let testGetAllAndFilter = async (): Promise<void> => {
    const contextProvider = Bit.DependencyManager.getCurrent().resolveObject<Bit.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const parentEntities = await context.parentEntities.filter(pe => pe.Name == "A")
        .toArray();
    if (parentEntities.length != 1) {
        throw new Error("parent entities are not loaded correctly");
    };
};

let testBatchReadRequest = async (): Promise<void> => {
    const contextProvider = Bit.DependencyManager.getCurrent().resolveObject<Bit.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const results = await context.batchExecuteQuery([context.parentEntities.take(1), context.testModels.take(1)]);
    if (results[0].length != 1) {
        throw new Error("There is a problem in batch read");
    }

    if (results[1].length != 1) {
        throw new Error("There is a problem in batch read");
    }
};

const testSync = async (): Promise<void> => {

    const contextProvider = Bit.DependencyManager.getCurrent().resolveObject<Bit.Contracts.IEntityContextProvider>("EntityContextProvider");
    const syncService = Bit.DependencyManager.getCurrent().resolveObject<Bit.Contracts.ISyncService>("SyncService");
    const guidUtils = Bit.DependencyManager.getCurrent().resolveObject<Bit.Implementations.DefaultGuidUtils>("GuidUtils");

    syncService.init(() => contextProvider.getContext<TestContext>("Test"), () => contextProvider.getContext<TestContext>("Test", { isOffline: true }));
    syncService.addEntitySetConfig<TestContext>({ name: "testCustomers", dtoType: BitTestsModel.TestCustomerDto });

    const offlineContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true });
    const onlineContext = await contextProvider.getContext<TestContext>("Test");

    onlineContext.testCustomers.addMany([{
        Id: guidUtils.newGuid(),
        Name: "A1",
        CityId: "EF529174-C497-408B-BB4D-C31C205D46BB"
    }, {
        Id: guidUtils.newGuid(),
        Name: "A2",
        CityId: "EF529174-C497-408B-BB4D-C31C205D46BB"
    }]);

    await onlineContext.saveChanges();

    await syncService.syncContext();

    const e = await offlineContext.testCustomers.first();

    offlineContext.attach(e);

    e.Name += "?";

    await offlineContext.saveChanges();

    onlineContext.testCustomers.addMany([{
        Id: guidUtils.newGuid(),
        Name: "A3",
        CityId: "EF529174-C497-408B-BB4D-C31C205D46BB"
    }, {
        Id: guidUtils.newGuid(),
        Name: "A4",
        CityId: "EF529174-C497-408B-BB4D-C31C205D46BB"
    }]);

    await onlineContext.saveChanges();

    await syncService.syncContext();

    offlineContext.remove(e);
    await offlineContext.saveChanges();

    await syncService.syncContext();
}