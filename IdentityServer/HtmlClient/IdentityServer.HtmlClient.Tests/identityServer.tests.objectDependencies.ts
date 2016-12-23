/// <reference path="../../../../bit-framework/Foundation/htmlclient/foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="../identityserver.htmlclient/identityserver.htmlclient.d.ts" />
/// <reference path="implementations/identityservertestpathprovider.ts" />

module IdentityServerTests {

    let dependencyManager = Foundation.Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "ModelProvider", type: IdentityServer.ViewModel.Implementations.DefaultModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: IdentityServer.ViewModel.Implementations.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "Logger", type: Foundation.ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Foundation.ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Foundation.ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Foundation.ViewModel.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager", instance: Foundation.Core.ClientAppProfileManager.getCurrent() });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Foundation.ViewModel.Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Foundation.ViewModel.Implementations.DefaultEntityContextProvider });
}