/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    export class DefaultAngularAppInitialization implements Core.Contracts.IAppEvents {

        protected getBaseModuleDependencies(): Array<string> {
            return [];
        }

        protected async onAppRun(app: angular.IModule): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();

            app.run(['$compile', '$document', '$location', '$rootScope', '$exceptionHandler', '$timeout', '$interval', '$parse', '$filter', '$http', '$cacheFactory', ($compile: angular.ICompileService, $document: ng.IDocumentService, $location: ng.ILocationService, $rootScope: ng.IRootScopeService, $exceptionHandler: ng.IExceptionHandlerService, $timeout: ng.ITimeoutService, $interval: ng.IIntervalService, $parse: angular.IParseService, $filter: ng.IFilterService, $http: ng.IHttpService, $cacheFactory: ng.ICacheFactoryService) => {
                dependencyManager.registerInstanceDependency({ instance: $exceptionHandler, name: "$exceptionHandler" });
                dependencyManager.registerInstanceDependency({ instance: $rootScope, name: "$rootScope" });
                dependencyManager.registerInstanceDependency({ instance: $document, name: "$document" });
                dependencyManager.registerInstanceDependency({ instance: $location, name: "$location" });
                dependencyManager.registerInstanceDependency({ instance: $timeout, name: "$timeout" });
                dependencyManager.registerInstanceDependency({ instance: $interval, name: "$interval" });
                dependencyManager.registerInstanceDependency({ instance: $parse, name: "$parse" });
                dependencyManager.registerInstanceDependency({ instance: $compile, name: "$compile" });
                dependencyManager.registerInstanceDependency({ instance: app, name: "app" });
                dependencyManager.registerInstanceDependency({ instance: $filter, name: "$filter" });
                dependencyManager.registerInstanceDependency({ instance: $http, name: "$http" });
                dependencyManager.registerInstanceDependency({ instance: $cacheFactory, name: '$cacheFactory' });
            }]);

        }

        protected async registerValues(app: angular.IModule): Promise<void> {
            app.value("$routerRootComponent", "app");
        }

        protected async registerComponents(app: any): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();

            const pathProvider = dependencyManager.resolveObject<Contracts.IPathProvider>("PathProvider");

            const formViewModelDependencies = dependencyManager.getAllFormViewModelsDependencies();

            const securityService = dependencyManager.resolveObject<Foundation.Core.Contracts.ISecurityService>("securityService");

            formViewModelDependencies.forEach(vm => {

                let original$routerOnActivate = vm.classCtor.prototype.$routerOnActivate;

                let original$routerCanActivate = vm.classCtor.prototype.$routerCanActivate;

                vm.classCtor.prototype.$routerOnActivate = async function () {

                    let canActivate = original$routerCanActivate == null || await original$routerCanActivate.apply(this, arguments);

                    if (canActivate == false)
                        throw new Error("Can't activate view model");

                    if (original$routerOnActivate != null) {
                        return await original$routerOnActivate.apply(this, arguments);
                    }
                };

            });

            formViewModelDependencies.forEach(vm => {

                let component: any = {
                    name: vm.name,
                    controller: vm.classCtor,
                    require: vm.require,
                    template: vm.template,
                    transclude: vm.transclude,
                    $canActivate: vm.$canActivate,
                    $routeConfig: vm.$routeConfig
                };

                if (vm.templateUrl != null)
                    component.templateUrl = pathProvider.getFullPath(vm.templateUrl);

                component.controllerAs = vm.controllerAs || "vm";

                component.bindings = angular.extend(vm.bindings || {}, { $router: '<' });

                app.component(vm.componentName, component);

            });

            dependencyManager.getAllComponentDependencies().forEach(component => {

                component.controllerAs = component.controllerAs || "vm";

                if (component.templateUrl != null)
                    component.templateUrl = pathProvider.getFullPath(component.templateUrl)

                app.component(component.name, component);

            });

            const routes = formViewModelDependencies
                .filter(vm => vm.locatedInMainRoute == true)
                .map(vm => {
                    let result: any = { path: vm.routeTemplate, component: vm.componentName, name: vm.name, useAsDefault: vm.useAsDefault };
                    return result;
                });

            app.component("app", {
                templateUrl: pathProvider.getFullPath(Core.ClientAppProfileManager.getCurrent().clientAppProfile.clientAppTemplateUrl),
                $routeConfig: routes,
                bindings: { $router: '<' }
            });

        }

        protected async buildAppModule(): Promise<angular.IModule> {

            const baseModuleDependencies = this.getBaseModuleDependencies();

            const app = angular.module(Core.ClientAppProfileManager.getCurrent().clientAppProfile.appName, baseModuleDependencies);

            return app;
        }

        protected async configureAppModule(app: angular.IModule): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();

            function extendExceptionHandler($delegate) {

                return (exception, cause) => {

                    $delegate(exception, cause);
                    const logger = dependencyManager.resolveObject<Core.Contracts.ILogger>("Logger");
                    logger.logError(exception.message || exception, cause, exception);
                };
            }

            app.config(["$provide", $provide => {
                $provide.decorator("$exceptionHandler",
                    ["$delegate", extendExceptionHandler]);
            }]);

            app.decorator('mdSwitchDirective', ['$delegate', function mdSwitchDecorator($delegate: ng.ISCEDelegateService) {

                let directive = ($delegate[0] as ng.IDirective);

                let originalCompile = directive.compile;

                directive.compile = function (element, attr) {

                    let result = originalCompile.apply(this, arguments);

                    let mdInputContainerParent = element.parent('md-input-container')

                    if (mdInputContainerParent.length != 0) {

                        mdInputContainerParent.addClass('md-input-has-value');

                    }

                    return result;
                }

                return $delegate;
            }]);

            let angularConfigs = dependencyManager.resolveAllObjects<Contracts.IAngularConfiguration>("AngularConfiguration");

            for (let i = 0; i < angularConfigs.length; i++) {
                await angularConfigs[i].configure(app);
            }
        }

        protected async registerDirectives(app: angular.IModule): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();
            const pathProvider = dependencyManager.resolveObject<Contracts.IPathProvider>("PathProvider");

            dependencyManager.getAllDirectivesDependencies()
                .map(d => { return { name: d.name, instance: Reflect.construct(d.classCtor as Function, []) as Contracts.IDirective }; })
                .forEach(directive => {

                    let originalGetDirectiveFactory = directive.instance.getDirectiveFactory();

                    let modifiedGetDirectiveFactory = function () {

                        let directiveResult: ng.IDirective = originalGetDirectiveFactory.apply(this, arguments);

                        if (directiveResult.templateUrl != null) {
                            directiveResult.templateUrl = pathProvider.getFullPath(directiveResult.templateUrl)
                        }

                        return directiveResult;

                    }

                    app.directive(directive.name, modifiedGetDirectiveFactory);
                });
        }

        protected async registerFilters(app: angular.IModule): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();

            const pathProvider = dependencyManager.resolveObject<Contracts.IPathProvider>("PathProvider");

            app.filter('date', () => {

                let dateTimeService = dependencyManager.resolveObject<Contracts.IDateTimeService>("DateTimeService");

                return function (date: Date): string {

                    return dateTimeService.getFormattedDate(date);

                }
            });

            app.filter('dateTime', () => {

                let dateTimeService = dependencyManager.resolveObject<Contracts.IDateTimeService>("DateTimeService");

                return function (date: Date): string {

                    return dateTimeService.getFormattedDateTime(date);

                }
            });

            app.filter('trusted', ['$sce', function ($sce) {
                return function (url) {
                    return $sce.trustAsResourceUrl(url);
                };
            }]);

            app.filter('files', () => {

                return (path: string): string => {

                    return pathProvider.getFullPath(path);

                }

            });
        }

        @Core.Log()
        public async onAppStartup(): Promise<void> {

            return new Promise<void>((res, rej) => {

                angular.element(document.body).ready(async () => {

                    try {

                        const dependencyManager = Core.DependencyManager.getCurrent();

                        let app = await this.buildAppModule();

                        await this.registerValues(app);

                        await this.configureAppModule(app);

                        await this.registerComponents(app);

                        await this.registerFilters(app);

                        await this.registerDirectives(app);

                        await this.onAppRun(app);

                        angular.bootstrap(document.body, [Core.ClientAppProfileManager.getCurrent().clientAppProfile.appName], {
                            strictDi: true
                        });

                        res();
                    }
                    catch (e) {
                        rej(e);
                        throw e;
                    }

                });

            });
        }
    }
}