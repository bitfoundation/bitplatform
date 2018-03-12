/// <reference path="imports.ts" />

module BitChangeSetManagerSecurity {

    const dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "ModelProvider", type: Bit.Implementations.Identity.DefaultModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.Identity.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "Logger", type: Bit.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Bit.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Bit.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Bit.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, Bit.ClientAppProfileManager.getCurrent());

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Bit.Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "SecurityService", type: Bit.Implementations.DefaultSecurityService });

    dependencyManager.registerObjectDependency({ name: "GuidUtils", type: Bit.Implementations.DefaultGuidUtils });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Bit.Implementations.DefaultEntityContextProvider });
}