/// <reference path="../foundation.view.htmlclient/foundation.view.d.ts" />
/// <reference path="implementations/testdefaultangularappinitialization.ts" />
module Foundation.Test {

    let dependencyManager = Core.DependencyManager.getCurrent();

    dependencyManager.registerObjectDependency({ name: "AppEvent", classCtor: Foundation.ViewModel.Implementations.DefaultDataSourceExtender });

    dependencyManager.registerObjectDependency({ name: "Logger", classCtor: Foundation.ViewModel.Implementations.DefaultLogger });

    dependencyManager.registerObjectDependency({ name: "AppStartup", classCtor: Foundation.ViewModel.Implementations.DefaultAppStartup });

    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", classCtor: Foundation.ViewModel.Implementations.DefaultEntityContextProvider });

    dependencyManager.registerObjectDependency({ name: "MessageReciever", classCtor: Foundation.ViewModel.Implementations.SignalRMessageReciever });

    dependencyManager.registerObjectDependency({ name: "MetadataProvider", classCtor: Foundation.ViewModel.Implementations.DefaultMetadataProvider });

    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", classCtor: Foundation.ViewModel.Implementations.DefaultAngularTranslateConfiguration });

    dependencyManager.registerObjectDependency({ name: "DateTimeService", classCtor: Foundation.ViewModel.Implementations.DefaultDateTimeService });

    dependencyManager.registerInstanceDependency({ name: 'ClientAppProfileManager', instance: Foundation.Core.ClientAppProfileManager.getCurrent() });
}