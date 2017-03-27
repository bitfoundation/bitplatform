let testComplexTypeWithOData = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    const complexDto = context.testComplex.add({ EntityId: 0, ComplexObj: { Name: "Test" } });

    await context.saveChanges();

    expect(complexDto.ComplexObj.Name).toBe("Test?");

    let complexDtoLoadedFromServer = await context.testComplex.find(1);

    context.attach(complexDtoLoadedFromServer, $data.EntityAttachMode.AllChanged);

    complexDtoLoadedFromServer.ComplexObj.Name = "Test";

    await context.saveChanges();

    expect(complexDtoLoadedFromServer.ComplexObj.Name).toBe("Test?");

    complexDtoLoadedFromServer = await context.testComplex.doSomeThingWithComplexObj(complexDtoLoadedFromServer);

    expect(complexDtoLoadedFromServer.ComplexObj.Name).toBe("Test??");

    expect((await context.testComplex.getComplexObjects().first()).Name).toBe("Test");
};

let testComplexTypeWithOfflineDb = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const insertContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true });

    let complexDto = insertContext.testComplex.add({ EntityId: 1, ComplexObj: { Name: "Test" } });

    await insertContext.saveChanges();

    const updateContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true });

    complexDto = await updateContext.testComplex.find(1);

    updateContext.attach(complexDto, $data.EntityAttachMode.AllChanged);

    complexDto.ComplexObj.Name = "Test2";

    await updateContext.saveChanges();

    const readContext = await contextProvider.getContext<TestContext>("Test", { isOffline: true });

    complexDto = await readContext.testComplex.find(1);

    expect(complexDto.ComplexObj.Name).toBe("Test2");
};

let simpleArrayValuesTest = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    const values = await context.testComplex.getValues([1, 2]).toArray();

    expect(values.map(v => v)).toEqual([2, 1]);
};

let enumTest = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    const dtoWithEnum = await context.dtoWithEnum.getDtoWithEnumsByGender(Foundation.Test.Model.Dto.TestGender.Man).first();

    expect(dtoWithEnum.Gender).toBe(Foundation.Test.Model.Dto.TestGender.Man);

    const dtoWithEnum2 = await context.dtoWithEnum.getDtoWithEnumsByGender2(Foundation.Test.Model.Dto.TestGender2.Man).first();

    expect(dtoWithEnum2.Test).toBe("Man");

    expect((await context.dtoWithEnum.postDtoWithEnum(dtoWithEnum))).toBe(true);

    await context.batchExecuteQuery([context.dtoWithEnum.getDtoWithEnumsByGender2(Foundation.Test.Model.Dto.TestGender2.Man), context.dtoWithEnum.getDtoWithEnumsByGender(Foundation.Test.Model.Dto.TestGender.Man)]);

    const enumsArrayTest = await context.dtoWithEnum.testEnumsArray([Foundation.Test.Model.Dto.TestGender2.Man, Foundation.Test.Model.Dto.TestGender2.Man]);

    expect(enumsArrayTest).toBe(true);
};