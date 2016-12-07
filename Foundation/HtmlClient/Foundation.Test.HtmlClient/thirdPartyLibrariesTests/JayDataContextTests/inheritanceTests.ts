let testInheritance = async (): Promise<void> => {

    const dependencyManager = Foundation.Core.DependencyManager.getCurrent();
    const guidUtils = dependencyManager.resolveObject<Foundation.ViewModel.Implementations.GuidUtils>("GuidUtils");
    const contextProvider = dependencyManager.resolveObject<Foundation.ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
    const odataContext = await contextProvider.getContext<TestContext>("Test");

    let newInheritedDtoInstance = new Foundation.Test.Model.Dto.SampleInheritedDto();

    expect(newInheritedDtoInstance instanceof Foundation.Test.Model.Dto.SampleBaseDto).toBeTruthy();

    newInheritedDtoInstance = (await odataContext.sampleInherited.getSampleDto());

    expect(newInheritedDtoInstance instanceof Foundation.Test.Model.Dto.SampleBaseDto).toBeTruthy();
    expect(newInheritedDtoInstance instanceof Foundation.Test.Model.Dto.SampleInheritedDto).toBeTruthy();
    expect(newInheritedDtoInstance.LastName).toBe("1");
    expect(newInheritedDtoInstance.Name).toBe("1");
}