let testDesiredEnvironmentsConfigsArePresentInClientSide = async (): Promise<void> => {
    if (Bit.DependencyManager.getCurrent().resolveObject<Bit.ClientAppProfileManager>("ClientAppProfileManager").getClientAppProfile().getConfig<boolean>("ClientSideAccessibleConfigTest") != true)
        throw new Error("There is an error in environment configs");
};