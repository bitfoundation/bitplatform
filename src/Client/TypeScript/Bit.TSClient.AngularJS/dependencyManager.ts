module Bit {

    export interface IDependency {
        name: string;
        predicate?: (clientAppInfo: Contracts.IClientAppProfile) => boolean;
        overwriteExisting?: boolean;
    }

    export interface IFileDependency extends IDependency {
        path: string;
        loadTime?: "Deferred" | "Early";
        fileDependencyType?: "Script" | "Style";
        loadStatus?: "IsBeingLoaded" | "NotLoaded" | "Loaded" | "LoadError";
        promise?: Promise<void>;
        continueOnError?: boolean;
        onLoad?: () => void;
        onError?: () => void;
    }

    export interface IComponentDependency extends IDependency, ng.IComponentOptions {
        type?: Function;
        cache?: boolean;
    }

    export interface IDirectiveDependency extends IDependency, ng.IDirective {
        type?: Function;
    }

    export interface IObjectDependency extends IDependency {
        type?: Function;
        lifeCycle?: "SingleInstance" | "Transient";
        resolver?: () => any;
    }

    export interface ICustomObjectResolver {
        canResolve: (name: string) => boolean;
        resolve: <T>(name: string) => T;
    }

    export class DependencyManager {

        private fileDependencies = new Array<IFileDependency>();

        private objectDependencies = new Array<IObjectDependency>();

        private componentDependencies = new Array<IComponentDependency>();

        private directiveDependencies = new Array<IDirectiveDependency>();

        private customObjectResolvers = new Array<ICustomObjectResolver>();

        private clientAppProfile = ClientAppProfileManager.getCurrent().getClientAppProfile();

        private dependencyShouldBeConsidered(dependency: IDependency): boolean {
            return dependency.predicate == null || dependency.predicate(this.clientAppProfile) == true;
        }

        public registerCustomObjectResolver(customObjectResolver: ICustomObjectResolver) {
            if (customObjectResolver == null) {
                throw new Error("custom object resolver may not be null");
            }
            if (customObjectResolver.resolve == null) {
                throw new Error("custom object resolver's resolve method may not be null");
            }
            this.customObjectResolvers.push(customObjectResolver);
        }

        public registerFileDependency(fileDependency: IFileDependency): void {

            if (fileDependency == null) {
                throw new Error("fileDependency is null");
            }

            if (fileDependency.name == null || fileDependency.name == "") {
                throw new Error("fileDependency's name is null or empty");
            }

            const dependenciesWithThisName = this.fileDependencies.filter(d => d.name.toLowerCase() == fileDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.fileDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (fileDependency.loadTime == null) {
                fileDependency.loadTime = "Early";
            }

            if (fileDependency.fileDependencyType == null) {
                fileDependency.fileDependencyType = "Script";
            }

            if (fileDependency.continueOnError == null)
                fileDependency.continueOnError = true;

            fileDependency.loadStatus = "NotLoaded";

            if (dependenciesWithThisNameIndex != -1) {
                if (fileDependency.overwriteExisting == true) {
                    this.fileDependencies[dependenciesWithThisNameIndex] = fileDependency;
                } else {
                    throw new Error(`Duplicated file dependency ${fileDependency.name}`);
                }
            }
            else {
                this.fileDependencies.push(fileDependency);
            }
        }

        public registerInstanceDependency(objectDep: IObjectDependency, instance: any): void {

            if (objectDep == null) {
                throw new Error("objectDep is null");
            }

            if (objectDep.name == null) {
                throw new Error("objectDep's name is null or empty");
            }

            if (instance == null) {
                throw new Error("instance may not be null");
            }

            if (!this.dependencyShouldBeConsidered(objectDep)) {
                return;
            }

            objectDep.resolver = () => {
                return instance;
            };

            this.registerObjectDependency(objectDep);
        }

        public registerObjectDependency(objectDependency: IObjectDependency): void {

            if (objectDependency == null) {
                throw new Error("objectDependency is null");
            }

            if (objectDependency.type == null && objectDependency.resolver == null) {
                throw new Error("Either provide type or resolver for your object dependency");
            }

            if (objectDependency.name == null || objectDependency.name == "") {
                throw new Error("objectDependency's name is null or empty");
            }

            if (!this.dependencyShouldBeConsidered(objectDependency)) {
                return;
            }

            const dependenciesWithThisName = this.objectDependencies.filter(d => d.name.toLowerCase() == objectDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.objectDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (objectDependency.lifeCycle == null) {
                objectDependency.lifeCycle = "SingleInstance";
            }

            if (objectDependency.resolver == null) {
                if (objectDependency.lifeCycle == "SingleInstance") {
                    objectDependency.resolver = () => {
                        if (objectDependency["instance"] == null) {
                            objectDependency["instance"] = Reflect.construct(objectDependency.type as Function, []);
                        }
                        return objectDependency["instance"];
                    };
                } else if (objectDependency.lifeCycle == "Transient") {
                    objectDependency.resolver = () => {
                        return Reflect.construct(objectDependency.type as Function, []);
                    };
                } else {
                    throw new Error(`Lifecycle ${objectDependency.lifeCycle} is not supported for ${objectDependency.name}`);
                }
            }

            if (dependenciesWithThisNameIndex != -1 && objectDependency.overwriteExisting == true) {
                this.objectDependencies[dependenciesWithThisNameIndex] = objectDependency;
            } else {
                this.objectDependencies.push(objectDependency);
            }
        }

        public registerDirectiveDependency(directiveDependency: IDirectiveDependency): void {

            if (directiveDependency == null) {
                throw new Error("directiveDependency is null");
            }

            if (directiveDependency.type == null) {
                throw new Error("directive dependency's type may not be null");
            }

            if (directiveDependency.name == null || directiveDependency.name == "") {
                throw new Error("directiveDependency's name is null or empty");
            }

            if (!this.dependencyShouldBeConsidered(directiveDependency)) {
                return;
            }

            directiveDependency.name = camelize(directiveDependency.name);
            directiveDependency.controller = directiveDependency.type as any;

            const dependenciesWithThisName = this.directiveDependencies.filter(d => d.name.toLowerCase() == directiveDependency.name.toLowerCase());

            let dependenciesWithThisNameIndex = -1;

            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.directiveDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (dependenciesWithThisNameIndex != -1) {
                if (directiveDependency.overwriteExisting == true) {
                    this.directiveDependencies[dependenciesWithThisNameIndex] = directiveDependency;
                } else {
                    throw new Error(`Duplicated directive dependency ${directiveDependency.name}`);
                }
            } else {
                this.directiveDependencies.push(directiveDependency);
            }
        }

        public registerComponentDependency(componentDependency: IComponentDependency): void {

            if (componentDependency == null) {
                throw new Error("componentDependency is null");
            }

            if (componentDependency.type == null) {
                throw new Error("component dependency's type may not be null");
            }

            if (componentDependency.name == null) {
                throw new Error("component dependency's name may not be null");
            }

            if (componentDependency.cache == null) {
                componentDependency.cache = false;
            }

            if (!this.dependencyShouldBeConsidered(componentDependency)) {
                return;
            }

            componentDependency.name = camelize(componentDependency.name);
            componentDependency.controller = componentDependency.type as any;
            componentDependency.controllerAs = componentDependency.controllerAs || "vm";

            const dependenciesWithThisName = this.componentDependencies.filter(d => d.name.toLowerCase() == componentDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.componentDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (dependenciesWithThisNameIndex != -1) {
                if (componentDependency.overwriteExisting == true) {
                    this.componentDependencies[dependenciesWithThisNameIndex] = componentDependency;
                } else {
                    throw new Error(`Duplicated component dependency ${componentDependency.name}`);
                }
            } else {
                this.componentDependencies.push(componentDependency);
            }

            if (componentDependency.cache == true) {

                let cacheComponent: IComponentDependency = {} as any;

                for (const prp in componentDependency) {
                    cacheComponent[prp] = componentDependency[prp];
                }

                cacheComponent.name = componentDependency.name + "Cache";
                cacheComponent.cache = false;
                cacheComponent.controller = cacheComponent.type = Components.CacheComponent;
                cacheComponent.controllerAs = "vm";
                cacheComponent.overwriteExisting = false;
                cacheComponent.predicate = null;
                cacheComponent.template = `<cache component-name="${componentDependency.name}"></cache>`;
                delete cacheComponent.templateUrl;

                this.registerComponentDependency(cacheComponent);
            }
        }

        private static current = new DependencyManager();

        public static getCurrent(): DependencyManager {
            return DependencyManager.current;
        }

        private loadInitialFileDependecies(files: Array<IFileDependency>) {

            const loadInitialFileDependency = (nextFile: IFileDependency) => {

                if (nextFile == null) {
                    const app = this.resolveObject<Contracts.IAppStartup>("AppStartup");
                    app.configuration();
                    return;
                }

                if (this.dependencyShouldBeConsidered(nextFile) == false) {
                    loadInitialFileDependency(files.shift());
                    return;
                }

                let element: HTMLScriptElement & HTMLLinkElement = null;

                if (nextFile.fileDependencyType == "Script") {
                    element = document.createElement("script") as HTMLScriptElement & HTMLLinkElement;
                    element.type = "text/javascript";
                    element.src = nextFile.path;
                    element.async = false;
                } else {
                    element = document.createElement("link") as HTMLScriptElement & HTMLLinkElement;
                    element.rel = "stylesheet";
                    element.type = "text/css";
                    element.href = nextFile.path;
                    element.async = false;
                }

                nextFile.loadStatus = "IsBeingLoaded";

                element.onload = (): void => {

                    element.onload = null;

                    if (nextFile.onLoad != null) {
                        nextFile.onLoad();
                    }

                    nextFile.loadStatus = "Loaded";

                    loadInitialFileDependency(files.shift());
                };

                element.onerror = (e): void => {

                    element.onload = null;

                    if (nextFile.onError != null) {
                        nextFile.onError();
                    }

                    nextFile.loadStatus = "LoadError";

                    if (nextFile.continueOnError == false) {
                        throw e;
                    }

                    loadInitialFileDependency(files.shift());
                };

                document.head.appendChild(element);
            }

            loadInitialFileDependency(files.shift());

        }

        public init(): void {

            Implementations.DefaultProvider.buildProvider();

            this.fileDependencies.forEach(fileDependency => {

                let path = fileDependency.path;

                if (!Implementations.PathUtils.isUrlPath(path)) {
                    let ext = "js";
                    if (fileDependency.fileDependencyType == "Style") {
                        ext = "css";
                    }
                    path += `.${ext}`;
                    path = `Files/V${this.clientAppProfile.version}/${path}`;
                }

                fileDependency.path = path;
            });

            const toBeLoadedAtFirstDependencies = this.fileDependencies
                .filter(fileDependency => fileDependency.loadTime == "Early");

            this.loadInitialFileDependecies(toBeLoadedAtFirstDependencies);
        }

        public resolveFile(fileDependencyName: string): Promise<void> {

            if (fileDependencyName == null || fileDependencyName == "") {
                throw new Error("argument exception: fileDependencyName");
            }

            const fileDepsWithThisName = this.fileDependencies
                .filter(dep => dep.name.toLowerCase() == fileDependencyName.toLowerCase());

            if (fileDepsWithThisName.length == 0) {
                throw new Error(`file dependency ${fileDependencyName} could not be found`);
            }

            const fileDependency = fileDepsWithThisName[0];

            if (fileDependency.loadTime == "Early") {
                throw new Error(`${fileDependencyName} file dependency was loaded at app startup`);
            }

            if (fileDependency.loadStatus != "NotLoaded") {
                return fileDependency.promise;
            }

            fileDependency.loadStatus = "IsBeingLoaded";

            fileDependency.promise = new Promise<void>((resolve, reject) => {

                if (this.dependencyShouldBeConsidered(fileDependency) == false) {
                    reject("File dependency may not be loaded because of its predicate");
                    return;
                }

                try {

                    let element: HTMLElement = null;

                    if (fileDependency.fileDependencyType == "Script") {
                        element = document.createElement("script");
                        (element as HTMLScriptElement).type = "text/javascript";
                        (element as HTMLScriptElement).src = fileDependency.path;
                        (element as HTMLScriptElement).async = false;
                    } else {
                        element = document.createElement("link");
                        (element as HTMLLinkElement).rel = "stylesheet";
                        (element as HTMLLinkElement).type = "text/css";
                        (element as HTMLLinkElement).href = fileDependency.path;
                    }

                    fileDependency.loadStatus = "IsBeingLoaded";

                    element.onload = (): void => {

                        element.onload = null;

                        if (this.clientAppProfile.isDebugMode == true) {
                            console.trace(`${fileDependency.name} loaded`);
                        }

                        fileDependency.loadStatus = "Loaded";
                        resolve();

                    };

                    element.onerror = (err): void => {

                        element.onerror = null;

                        fileDependency.loadStatus = "LoadError";
                        reject(err);
                    };

                    document.head.appendChild(element);

                } catch (e) {
                    fileDependency.loadStatus = "LoadError";
                    reject(e);
                    throw e;
                }

            });

            return fileDependency.promise;
        }

        public resolveObject<TService>(objectDependencyName: string): TService {

            if (objectDependencyName == null || objectDependencyName == "") {
                throw new Error("argument exception: objectDependencyName");
            }

            let result = this.resolveAllObjects<TService>(objectDependencyName)[0];

            if (result == null) {
                for (let customObjectResolver of this.customObjectResolvers) {
                    let canResolve = false;
                    try {
                        canResolve = customObjectResolver.canResolve == null || customObjectResolver.canResolve(objectDependencyName);
                    } catch (e) { }
                    if (canResolve == true) {
                        result = customObjectResolver.resolve<TService>(objectDependencyName);
                        if (result != null) {
                            break;
                        }
                    }
                }
            }

            if (result == null) {
                throw new Error(`object dependency ${objectDependencyName} could not be found`);
            }

            return result;
        }

        public resolveAllObjects<TService>(objectDependencyName: string): Array<TService> {

            if (objectDependencyName == null || objectDependencyName == "") {
                throw new Error("argument exception: objectDependencyName");
            }

            const objectDepsWithThisName = this.objectDependencies
                .filter(dep => dep.name.toLowerCase() == objectDependencyName.toLowerCase());

            return objectDepsWithThisName.map(objDep => {
                return objDep.resolver();
            });
        }

        public getAllDirectivesDependencies(): Array<IDirectiveDependency> {
            return this.directiveDependencies;
        }

        public getAllComponentDependencies(): Array<IComponentDependency> {
            return this.componentDependencies;
        }
    }

    export function ObjectDependency(objectDependency: IObjectDependency): ClassDecorator {

        return (targetService: IObjectDependency & Function): any => {

            targetService = Injectable()(targetService) as IObjectDependency & Function;

            objectDependency.type = targetService;

            DependencyManager.getCurrent()
                .registerObjectDependency(objectDependency);

            return targetService;
        };
    }

    export function DtoRulesDependency(dtoRules: IObjectDependency): ClassDecorator {

        return (targetDtoRules: IObjectDependency & Function): any => {

            targetDtoRules = Injectable()(targetDtoRules) as IObjectDependency & Function;

            dtoRules.type = targetDtoRules;

            dtoRules.lifeCycle = "Transient";

            DependencyManager.getCurrent()
                .registerObjectDependency(dtoRules);

            return targetDtoRules;
        };
    }

    export function ComponentDependency(componentDependency: IComponentDependency): ClassDecorator {

        return (targetComponent: IComponentDependency & Function): any => {

            targetComponent = Injectable()(targetComponent) as IComponentDependency & Function;

            componentDependency.type = targetComponent;

            DependencyManager.getCurrent()
                .registerComponentDependency(componentDependency);

            return targetComponent;
        };
    }

    export function DtoViewModelDependency(dtoViewModel: IComponentDependency): ClassDecorator {

        return (targetDtoViewModel: IComponentDependency & Function): any => {

            targetDtoViewModel = Injectable()(targetDtoViewModel) as IComponentDependency & Function;

            dtoViewModel.bindings = dtoViewModel.bindings || {};

            if (dtoViewModel.bindings["model"] == null) {
                dtoViewModel.bindings["model"] = "<?";
            }

            dtoViewModel.type = targetDtoViewModel;

            DependencyManager.getCurrent()
                .registerComponentDependency(dtoViewModel);

            return targetDtoViewModel;
        };
    }

    export function DirectiveDependency(directiveDependency: IDirectiveDependency): ClassDecorator {

        return (targetDirective: IDirectiveDependency & Function): any => {

            targetDirective = Injectable()(targetDirective) as IDirectiveDependency & Function;

            directiveDependency.type = targetDirective;

            DependencyManager.getCurrent()
                .registerDirectiveDependency(directiveDependency);

            return targetDirective;
        };
    }

    export function Inject(name: string, use$inject = name == "$element" || name == "$scope" || name == "$attrs" || name == "$transclude"): ParameterDecorator {

        if (name == null || name == "") {
            throw new Error("name may not be null or empty");
        }

        return (target: Function, propertyKey: string | symbol): Function => {
            target.injects = target.injects || [];
            target.$inject = target.$inject || [];
            if (use$inject == false) {
                target.injects.push({ name: name, kind: "Single" });
            } else {
                (target.$inject as Array<string>).unshift(name);
            }
            return target;
        };
    }

    export function InjectAll(name: string): ParameterDecorator {

        if (name == null || name == "") {
            throw new Error("name may not be null or empty");
        }

        return (target: Function, propertyKey: string | symbol): Function => {
            target.injects = target.injects || [];
            target.injects.push({ name: name, kind: "All" });
            return target;
        }
    }

    export function Injectable(): ClassDecorator {

        return (target: Function): any => {

            const injects = target.injects;

            if (injects != null && injects.length != 0) {

                const originalTarget = target;

                target = function construct() {

                    const dependencyManager = DependencyManager.getCurrent();

                    const args = Array.from(arguments);

                    for (let inject of injects.slice(0).reverse()) {
                        if (inject.kind == "All") {
                            args.push(dependencyManager.resolveAllObjects<any[]>(inject.name));
                        } else {
                            args.push(dependencyManager.resolveObject<any>(inject.name));
                        }
                    }

                    return Reflect.construct(originalTarget, args);
                };

                for (let prp in originalTarget) {
                    if (originalTarget.hasOwnProperty(prp)) {
                        target[prp] = originalTarget[prp];
                    }
                }

                target.prototype = originalTarget.prototype;
            };

            return target;
        }
    }

    function camelize(str: string): string {
        return str.replace(/(?:^\w|[A-Z]|\b\w)/g, (letter, index) => (index == 0 ? letter.toLowerCase() : letter.toUpperCase())).replace(/\s+/g, "");
    }
}
