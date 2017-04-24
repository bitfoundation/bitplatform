/// <reference path="imports.ts" />

module BitChangeSetManager {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: FoundationVM.Implementations.DefaultKendoExtender });

    dependencyManager.registerObjectDependency({ name: "Logger", type: FoundationVM.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: FoundationVM.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: FoundationVM.Implementations.DefaultEntityContextProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: FoundationVM.Implementations.DefaultEntityContextProviderAppEvent });

    dependencyManager.registerObjectDependency({ name: "MessageReceiver", type: FoundationVM.Implementations.SignalRMessageReceiver });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: FoundationVM.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: FoundationVM.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: FoundationVM.Implementations.DefaultDateTimeService });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, FoundationCore.ClientAppProfileManager.getCurrent());
}