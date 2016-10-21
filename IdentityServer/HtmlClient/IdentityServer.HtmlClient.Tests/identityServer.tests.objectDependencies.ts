/// <reference path="../../../../bit-framework/Foundation/htmlclient/foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="../identityserver.htmlclient/identityserver.htmlclient.d.ts" />
/// <reference path="implementations/identityservertestpathprovider.ts" />

module IdentityServerTests {

    let dependencyManager = Foundation.Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "ModelProvider", classCtor: IdentityServer.ViewModel.Implementations.DefaultModelProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", classCtor: IdentityServer.ViewModel.Implementations.IdentityServerDefaultAngularAppInitialization });

    dependencyManager.registerObjectDependency({ name: "PathProvider", classCtor: IdentityServerTest.Implementations.IdentityServerTestPathProvider });

    dependencyManager.registerObjectDependency({ name: "Logger", classCtor: Foundation.ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", classCtor: Foundation.ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", classCtor: Foundation.ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", classCtor: Foundation.ViewModel.Implementations.DefaultAngularTranslateConfiguration });
}