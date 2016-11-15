let testFunctionCallAndTakeMethodAfterThat = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const testModels = await context.testModels.getTestModelsByStringPropertyValue('1').take(1).toArray();
    if (testModels.length != 1)
        throw new Error("test models are not loaded correctly");
};

let testActionCall = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const areEqual = await context.testModels.areEqual(10, 10);
    if (areEqual != true)
        throw new Error("are equal result is not valid");
};

let testBatchCallODataFunctions = async (): Promise<void> => {
    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    const results = await context.batchExecuteQuery([context.testModels.getTestModelsByStringPropertyValue('1').take(1), context.testModels.getTestModelsByStringPropertyValue('2').take(1)]);
    if (results[0].length != 1) {
        throw new Error("There is a problem in batch read");
    }

    if (results[1].length != 1) {
        throw new Error("There is a problem in batch read");
    }
};

let testPassingArrayOfEntitiesToController = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");
    let validations = [
        new Foundation.Test.Model.Dto.ValidationSampleDto({ Id: 1, RequiredByAttributeMember: 'A', RequiredByMetadataMember: 'B' }),
        new Foundation.Test.Model.Dto.ValidationSampleDto({ Id: 2, RequiredByAttributeMember: 'B', RequiredByMetadataMember: 'C' })
    ];

    let result = await context.validationSamples.submitValidations(validations).first();

    if (result.Id != "2" || result.RequiredByAttributeMember != "AA" || result.RequiredByMetadataMember != "BB")
        throw new Error('IEEE754Compatibility problem');

    result = await context.validationSamples.submitValidations(validations, 'A').first();

    if (result.Id != "2" || result.RequiredByAttributeMember != "AAA" || result.RequiredByMetadataMember != "BBA")
        throw new Error('IEEE754Compatibility problem');
}

let testIEEE754Compatibility = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    let result = await context.testModels.testIEEE754Compatibility("79228162514264337593543950335");

    if (result != "79228162514264337593543950335")
        throw new Error('IEEE754Compatibility problem');

    let result2 = await context.testModels.testIEEE754Compatibility2(2147483647);

    if (result2 != 2147483647)
        throw new Error('IEEE754Compatibility problem');

    let result3 = await context.testModels.testIEEE754Compatibility3("9223372036854775807");

    if (result3 != "9223372036854775807")
        throw new Error('IEEE754Compatibility problem');

    let result4 = await context.testModels.testIEEE754Compatibility("12.2");

    if (result4 != "12.2")
        throw new Error('IEEE754Compatibility problem');

    let result5 = await context.testModels.testIEEE754Compatibility("214748364711111.2");

    if (result5 != "214748364711111.2")
        throw new Error('IEEE754Compatibility problem');

    let result6 = await context.testModels.testIEEE754Compatibility("214748364711111");

    if (result6 != "214748364711111")
        throw new Error('IEEE754Compatibility problem');

    if (new Foundation.ViewModel.Implementations.DefaultMath().sum('123456789123456789', '123456789123456789') != (await context.testModels.testDecimalSum('123456789123456789', '123456789123456789'))) {
        throw new Error('IEEE754Compatibility problem');
    }
};