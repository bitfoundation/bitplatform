/// <reference path="../../../../bit-framework/Foundation/htmlclient/foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="../identityserver.htmlclient/identityserver.htmlclient.d.ts" />
/// <reference path="implementations/identityservertestpathprovider.ts" />

module IdentityServerTests {

    let dependencyManager = Foundation.Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "ModelProvider", class: IdentityServer.ViewModel.Implementations.DefaultModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", class: IdentityServer.ViewModel.Implementations.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "Logger", class: Foundation.ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", class: Foundation.ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", class: Foundation.ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", class: Foundation.ViewModel.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerInstanceDependency({ name: 'ClientAppProfileManager', instance: Foundation.Core.ClientAppProfileManager.getCurrent() });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", class: Foundation.ViewModel.Implementations.DefaultDateTimeService });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", class: Foundation.ViewModel.Implementations.DefaultEntityContextProvider });
}