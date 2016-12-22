
let testGetOfDtoSetController = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    const model = await context.testCustomers.first(c => c.Id == "28e1ff65-da41-4fa3-8aeb-5196494b407d");

    expect(model.Id).toBe("28e1ff65-da41-4fa3-8aeb-5196494b407d");
    expect(model.Name).toBe("TestCustomer");
    expect(model.CityId).toBe("ef529174-c497-408b-bb4d-c31c205d46bb");
    expect(model.CityName).toBe("TestCity");

};

let testPatchOfDtoSetController = async (): Promise<void> => {

    const contextProvider = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const context = await contextProvider.getContext<TestContext>("Test");

    let model = await context.testCustomers.first(c => c.Id == "28e1ff65-da41-4fa3-8aeb-5196494b407d");

    model.Name += "?";

    await model.save();

    model = await context.testCustomers.first(c => c.Id == "28e1ff65-da41-4fa3-8aeb-5196494b407d");

    expect(model.Name).toBe("TestCustomer?");
};