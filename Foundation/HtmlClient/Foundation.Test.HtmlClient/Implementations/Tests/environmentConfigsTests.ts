let testDesiredEnvironmentsConfigsArePresentInClientSide = async (): Promise<void> => {
    if (Foundation.Core.ClientAppProfileManager.getCurrent().clientAppProfile.getConfig<boolean>("ClientSideAccessibleConfigTest") != true)
        throw new Error("There is an error in environment configs");
};