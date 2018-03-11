module Bit.Tests.Identity {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "LoginModelProvider", type: Implementations.Identity.DefaultLoginModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Implementations.Identity.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "Logger", type: Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, ClientAppProfileManager.getCurrent());

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "SecurityService", type: Implementations.DefaultSecurityService });

    dependencyManager.registerObjectDependency({ name: "GuidUtils", type: Bit.Implementations.DefaultGuidUtils });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Implementations.DefaultEntityContextProvider });
}