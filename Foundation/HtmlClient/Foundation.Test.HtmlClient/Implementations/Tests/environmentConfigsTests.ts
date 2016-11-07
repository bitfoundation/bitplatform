let testDesiredEnvironmentsConfigsArePresentInClientSide = async (): Promise<void> => {
    if (Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.Core.ClientAppProfileManager>("ClientAppProfileManager").getClientAppProfile().getConfig<boolean>("ClientSideAccessibleConfigTest") != true)
        throw new Error("There is an error in environment configs");
};