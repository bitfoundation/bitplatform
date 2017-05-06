/// <reference path="../../../foundation.core.htmlclient/declarations.d.ts" />
let testSignalRConnection = async (): Promise<void> => {
    const dependencyManager = Foundation.Core.DependencyManager.getCurrent();
    const messageReceiver = dependencyManager.resolveObject<Foundation.Core.Contracts.IMessageReceiver>("MessageReceiver");
    messageReceiver.start();
};