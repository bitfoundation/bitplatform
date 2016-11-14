/// <reference path="../../test.model.context.d.ts" />
/// <reference path="../../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />
let testInsert = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const model = context.testModels.add({ StringProperty: "Test", DateProperty: new Date() });
    await context.saveChanges();

    if (model.Id == '0') {
        throw new Error("Id may not be zero");
    };
};

let testGetAllAndFilter = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getReadContext<TestContext>("Test");
    const parentEntities = await context.parentEntities.filter(pe => pe.Name == "A")
        .toArray();
    if (parentEntities.length != 1) {
        throw new Error("parent entities are not loaded correctly");
    };
};

let testBatchReadRequest = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getReadContext<TestContext>("Test");
    const results = await context.batchExecuteQuery([context.parentEntities.take(1), context.testModels.take(1)]);
    if (results[0].length != 1) {
        throw new Error("There is a problem in batch read");
    }

    if (results[1].length != 1) {
        throw new Error("There is a problem in batch read");
    }
};