/// <reference path="../foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="implementations/testdefaultangularappinitialization.ts" />
module Foundation.Test {

    let dependencyManager = Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: ViewModel.Implementations.DefaultKendoExtender });

    dependencyManager.registerObjectDependency({ name: "Logger", type: ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", type: ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: ViewModel.Implementations.DefaultEntityContextProvider });

    dependencyManager.registerObjectDependency({ name: "AppEvent", type: ViewModel.Implementations.DefaultEntityContextProviderAppEvent });

    dependencyManager.registerObjectDependency({ name: "MessageReceiver", type: ViewModel.Implementations.SignalRMessageReceiver });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: ViewModel.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: ViewModel.Implementations.DefaultDateTimeService });

    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, Core.ClientAppProfileManager.getCurrent());
}