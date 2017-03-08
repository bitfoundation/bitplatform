/// <reference path="imports.ts" />

module BitChangeSetManagerSecurity {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "ModelProvider", type: IdentityServerVM.Implementations.DefaultModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: IdentityServerVM.Implementations.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "Logger", type: FoundationVM.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: FoundationVM.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: FoundationVM.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: FoundationVM.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, FoundationCore.ClientAppProfileManager.getCurrent());

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: FoundationVM.Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: FoundationVM.Implementations.DefaultEntityContextProvider });
}