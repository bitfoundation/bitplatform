let testComplexTypeWithOData = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    let complexDto = context.testComplex.add({ EntityId: 0, ComplexObj: { Name: 'Test' } });

    await context.saveChanges();

    expect(complexDto.ComplexObj.Name).toBe('Test?');

    let complexDtoLoadedFromServer = await context.testComplex.find(1);

    context.attach(complexDtoLoadedFromServer, $data.EntityAttachMode.AllChanged);

    complexDtoLoadedFromServer.ComplexObj.Name = "Test";

    await context.saveChanges();

    expect(complexDtoLoadedFromServer.ComplexObj.Name).toBe("Test?");

    complexDtoLoadedFromServer = await context.testComplex.doSomeThingWithComplexObj(complexDtoLoadedFromServer);

    expect(complexDtoLoadedFromServer.ComplexObj.Name).toBe("Test??");
};

let testComplexTypeWithOfflineDb = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const insertContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true });

    let complexDto = insertContext.testComplex.add({ EntityId: 1, ComplexObj: { Name: 'Test' } });

    await insertContext.saveChanges();

    let updateContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true })

    complexDto = await updateContext.testComplex.find(1);

    updateContext.attach(complexDto, $data.EntityAttachMode.AllChanged);

    complexDto.ComplexObj.Name = "Test2";

    await updateContext.saveChanges();

    let readContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true })

    complexDto = await readContext.testComplex.find(1);

    expect(complexDto.ComplexObj.Name).toBe("Test2");
};