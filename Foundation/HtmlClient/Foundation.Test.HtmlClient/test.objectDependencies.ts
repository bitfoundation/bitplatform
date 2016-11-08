/// <reference path="../foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="implementations/testdefaultangularappinitialization.ts" />
module Foundation.Test {

    let dependencyManager = Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "AppEvent", class: Foundation.ViewModel.Implementations.DefaultKendoExtender });

    dependencyManager.registerObjectDependency({ name: "Logger", class: Foundation.ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", class: Foundation.ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", class: Foundation.ViewModel.Implementations.DefaultEntityContextProvider });

    dependencyManager.registerObjectDependency({ name: "MessageReciever", class: Foundation.ViewModel.Implementations.SignalRMessageReciever });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", class: Foundation.ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", class: Foundation.ViewModel.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", class: Foundation.ViewModel.Implementations.DefaultDateTimeService });

    dependencyManager.registerInstanceDependency({ name: 'ClientAppProfileManager', instance: Foundation.Core.ClientAppProfileManager.getCurrent() });
}