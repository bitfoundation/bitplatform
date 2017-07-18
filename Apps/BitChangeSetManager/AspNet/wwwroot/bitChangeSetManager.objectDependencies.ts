/// <reference path="imports.ts" />

module BitChangeSetManager {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.DefaultKendoExtender });

    dependencyManager.registerObjectDependency({ name: "Logger", type: Bit.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Bit.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Bit.Implementations.DefaultEntityContextProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.DefaultEntityContextProviderAppEvent });

    dependencyManager.registerObjectDependency({ name: "MessageReceiver", type: Bit.Implementations.SignalRMessageReceiver });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Bit.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Bit.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Bit.Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "SecurityService", type: Bit.Implementations.DefaultSecurityService });

    dependencyManager.registerObjectDependency({ name: "GuidUtils", type: Bit.Implementations.DefaultGuidUtils });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, Bit.ClientAppProfileManager.getCurrent());
}