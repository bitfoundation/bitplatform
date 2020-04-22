let testSignalRConnection = async (): Promise<void> => {
    const dependencyManager = Bit.DependencyManager.getCurrent();
    const messageReceiver = dependencyManager.resolveObject<Bit.Contracts.IMessageReceiver>("MessageReceiver");
    messageReceiver.start();
};