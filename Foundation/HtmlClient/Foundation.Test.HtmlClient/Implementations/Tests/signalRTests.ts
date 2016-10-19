/// <reference path="../../../foundation.core.htmlclient/declarations.d.ts" />
let testSignalRConnection = async (): Promise<void> => {
    const dependencyManager = Foundation.Core.DependencyManager.getCurrent();
    const messageReciever = dependencyManager.resolveObject<Foundation.Core.Contracts.IMessageReciever>("MessageReciever");
    await messageReciever.onMessageRecieved("Some-Task", async () => { });
};