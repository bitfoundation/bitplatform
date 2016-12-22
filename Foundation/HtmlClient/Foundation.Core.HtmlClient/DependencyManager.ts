/// <reference path="typings.d.ts" />
/// <reference path="clientappprofilemanager.ts" />
/// <reference path="contracts/iclientappprofile.ts" />

module Foundation.Core {

    export interface IDependency {
        name: string;
        predicate?: (clientAppInfo: Contracts.IClientAppProfile) => boolean;
        overwriteExisting?: boolean;
    }

    export interface IFileDependency extends IDependency {
        path: string;
        loadTime?: "Defered" | "Early";
        fileDependecyType?: "Script" | "Style";
        loadStatus?: "IsBeingLoaded" | "NotLoaded" | "Loaded";
        promise?: Promise<void>;
    }

    export interface IComponentDependency extends IDependency, ng.IComponentOptions {
        type?: Function;
    }

    export interface IFormViewModelDependency extends IComponentDependency {

    }

    export interface IDirectiveDependency extends IDependency {
        type?: Function;
    }

    export interface IObjectDependency extends IDependency {
        type?: Function;
        lifeCycle?: "SingleInstance" | "PerRoute" | "Transient";
    }

    export interface IInstanceDependency extends IDependency {
        instance: Object;
    }

    export interface ICustomObjectResolver {
        canResolve: (name: string) => boolean;
        resolve: <T>(name: string) => T;
    }

    export class DependencyManager {

        private fileDependencies = new Array<IFileDependency>();

        private objectDependencies = new Array<IObjectDependency>();

        private singletoneObjectDependenciesInstances = new Array<{ objectDep: IDependency, objectDepInstance: Object }>();

        private formViewModelDependencies = new Array<IFormViewModelDependency>();

        private componentDependencies = new Array<IComponentDependency>();

        private directiveDependencies = new Array<IDirectiveDependency>();

        private customObjectResolvers = new Array<ICustomObjectResolver>();

        private clientAppProfile = ClientAppProfileManager.getCurrent().getClientAppProfile();

        private dependencyShouldBeConsidered(dependency: IDependency): boolean {
            return dependency.predicate == null || dependency.predicate(this.clientAppProfile) == true;
        }

        public registerCustomObjectResolver(customObjectResolver: ICustomObjectResolver) {
            if (customObjectResolver == null)
                throw new Error("custom object resolver may not be null");
            if (customObjectResolver.resolve == null)
                throw new Error("custom object resolver`s resolve method may not be null");
            this.customObjectResolvers.push(customObjectResolver);
        }

        public registerFileDependency(fileDependency: IFileDependency): void {

            if (fileDependency == null)
                throw new Error("fileDependency is null");

            if (fileDependency.name == null || fileDependency.name == "")
                throw new Error("fileDependency.name is null or empty");

            const dependenciesWithThisName = this.fileDependencies.filter(d => d.name.toLowerCase() == fileDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.fileDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (fileDependency.loadTime == null)
                fileDependency.loadTime = "Early";

            if (fileDependency.fileDependecyType == null)
                fileDependency.fileDependecyType = "Script";

            fileDependency.loadStatus = "NotLoaded";

            if (dependenciesWithThisNameIndex != -1) {
                if (fileDependency.overwriteExisting == true)
                    this.fileDependencies[dependenciesWithThisNameIndex] = fileDependency;
                else
                    throw new Error("Duplicated file dependency " + fileDependency.name);
            }
            else {
                this.fileDependencies.push(fileDependency);
            }
        }

        public registerInstanceDependency(instanceDependency: IInstanceDependency, objectDep?: IObjectDependency): void {

            if (instanceDependency == null)
                throw new Error("instanceDependency is null");

            if (instanceDependency.name == null || instanceDependency.name == "")
                throw new Error("instanceDependency.name is null or empty");

            if (objectDep != null && objectDep.name.toLowerCase() != instanceDependency.name.toLowerCase())
                throw new Error(`objectDep's name must be equal to instanceDependency.name`);

            if (!this.dependencyShouldBeConsidered(instanceDependency))
                return;

            let singletoneObjDep = null;

            for (let d of this.singletoneObjectDependenciesInstances) {
                if ((objectDep != null && d.objectDep == objectDep) || d.objectDep.name.toLowerCase() == instanceDependency.name.toLowerCase())
                    singletoneObjDep = d;
            }

            if (singletoneObjDep != null && instanceDependency.overwriteExisting == true) {
                singletoneObjDep.objectDepInstance = instanceDependency.instance;
            }
            else {
                this.singletoneObjectDependenciesInstances.push({ objectDep: objectDep != null ? objectDep : instanceDependency, objectDepInstance: instanceDependency.instance });
            }
        }

        public registerObjectDependency(objectDependency: IObjectDependency): void {

            if (objectDependency == null)
                throw new Error("objectDependency is null");

            if (objectDependency.type == null)
                throw new Error("class of object dependency may not be null");

            if (objectDependency.name == null || objectDependency.name == "")
                throw new Error("objectDependency.name is null or empty");

            if (!this.dependencyShouldBeConsidered(objectDependency))
                return;

            const dependenciesWithThisName = this.objectDependencies.filter(d => d.name.toLowerCase() == objectDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.objectDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (objectDependency.lifeCycle == null)
                objectDependency.lifeCycle = "SingleInstance";

            if (dependenciesWithThisNameIndex != -1 && objectDependency.overwriteExisting == true) {
                this.objectDependencies[dependenciesWithThisNameIndex] = objectDependency
            }
            else {
                this.objectDependencies.push(objectDependency);
            }
        }

        public registerDirectiveDependency(directiveDependency: IDirectiveDependency): void {

            if (directiveDependency == null)
                throw new Error("directiveDependency is null");

            if (directiveDependency.type == null)
                throw new Error("class of directive dependency may not be null");

            if (directiveDependency.name == null || directiveDependency.name == "")
                throw new Error("directiveDependency.name is null or empty");

            if (!this.dependencyShouldBeConsidered(directiveDependency))
                return;

            const dependenciesWithThisName = this.directiveDependencies.filter(d => d.name.toLowerCase() == directiveDependency.name.toLowerCase());

            let dependenciesWithThisNameIndex = -1;

            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.directiveDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (dependenciesWithThisNameIndex != -1) {
                if (directiveDependency.overwriteExisting == true)
                    this.directiveDependencies[dependenciesWithThisNameIndex] = directiveDependency;
                else
                    throw new Error("Duplicated directive dependency " + directiveDependency.name);
            }
            else {
                this.directiveDependencies.push(directiveDependency);
            }
        }

        public registerComponentDependency(componentDependency: IComponentDependency): void {

            if (componentDependency == null)
                throw new Error("componentDependency is null");

            if (componentDependency.type == null)
                throw new Error("class of component dependency may not be null");

            if (componentDependency.name == null)
                throw new Error("name of component dependency may not be null");

            if (!this.dependencyShouldBeConsidered(componentDependency))
                return;

            componentDependency.name = camelize(componentDependency.name);
            componentDependency.controller = componentDependency.type as any;

            const dependenciesWithThisName = this.componentDependencies.filter(d => d.name.toLowerCase() == componentDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.componentDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (dependenciesWithThisNameIndex != -1) {
                if (componentDependency.overwriteExisting == true)
                    this.componentDependencies[dependenciesWithThisNameIndex] = componentDependency;
                else
                    throw new Error("Duplicated component dependency");
            }
            else {
                this.componentDependencies.push(componentDependency);
            }
        }

        public registerFormViewModelDependency(formViewModelDependency: IFormViewModelDependency): void {

            if (formViewModelDependency == null)
                throw new Error("formViewModelDependency is null");

            if (formViewModelDependency.type == null)
                throw new Error("class of viewModel dependency may not be null");

            if (formViewModelDependency.name == null)
                throw new Error("name of viewModel dependency may not be null");

            if (!this.dependencyShouldBeConsidered(formViewModelDependency))
                return;

            formViewModelDependency.name = camelize(formViewModelDependency.name);
            formViewModelDependency.controller = formViewModelDependency.type as any;

            if (formViewModelDependency.$routeConfig != null) {
                formViewModelDependency.$routeConfig.filter(r => r.name != null && r.component == null).forEach(r => {
                    r.component = camelize(r.name);
                });
            }

            const dependenciesWithThisName = this.formViewModelDependencies.filter(d => d.name.toLowerCase() == formViewModelDependency.name.toLowerCase());
            let dependenciesWithThisNameIndex = -1;
            if (dependenciesWithThisName.length == 1) {
                dependenciesWithThisNameIndex = this.formViewModelDependencies.indexOf(dependenciesWithThisName[0]);
            }

            if (dependenciesWithThisNameIndex != -1) {
                if (formViewModelDependency.overwriteExisting == true)
                    this.formViewModelDependencies[dependenciesWithThisNameIndex] = formViewModelDependency;
                else
                    throw new Error("Duplicated viewModel dependency " + formViewModelDependency.name);
            }
            else {
                this.formViewModelDependencies.push(formViewModelDependency);
            }
        }

        private static current = new DependencyManager();

        public static getCurrent(): DependencyManager {
            return DependencyManager.current;
        }

        private loadInitialFileDependecies(files: Array<IFileDependency>) {

            const loadInitialFileDependecy = (nextFile: IFileDependency) => {

                if (this.dependencyShouldBeConsidered(nextFile) == false) {

                    nextFile = files.shift();

                    if (nextFile != null) {
                        loadInitialFileDependecy(nextFile);
                    } else {
                        const app = this.resolveObject<Contracts.IAppStartup>("AppStartup");
                        app.configuration();
                    }

                    return;
                }

                let element = null;

                if (nextFile.fileDependecyType == "Script") {
                    element = document.createElement("script");
                    element.type = "text/javascript";
                    element.src = nextFile.path;
                } else {
                    element = document.createElement("link");
                    element.rel = "stylesheet";
                    element.type = "text/css";
                    element.href = nextFile.path;
                }

                nextFile.loadStatus = "IsBeingLoaded";

                element.onload = (): void => {

                    nextFile.loadStatus = "Loaded";
                    nextFile = files.shift();

                    if (nextFile != null) {
                        loadInitialFileDependecy(nextFile);
                    } else {
                        const app = this.resolveObject<Contracts.IAppStartup>("AppStartup");
                        app.configuration();
                    }

                };

                document.head.appendChild(element);
            };

            if (files.length != 0) {
                loadInitialFileDependecy(files.shift());
            }
            else {
                const app = this.resolveObject<Contracts.IAppStartup>("AppStartup");
                app.configuration();
            }
        }

        public init(): void {

            this.fileDependencies.forEach(fileDependency => {

                let path = fileDependency.path;

                let ext = "js";

                if (fileDependency.fileDependecyType == "Style") {
                    ext = "css";
                }

                path += `.${ext}`;

                if (path.indexOf("http") != 0)
                    path = `Files/V${this.clientAppProfile.version}/${path}`;

                fileDependency.path = path;
            });

            const toBeLoadedAtFirstDependencies = this.fileDependencies
                .filter(fileDependency => fileDependency.loadTime == "Early");

            this.loadInitialFileDependecies(toBeLoadedAtFirstDependencies);
        }

        public resolveFile(fileDependencyName: string): Promise<void> {

            if (fileDependencyName == null || fileDependencyName == "")
                throw new Error("argument exception: fileDependencyName");

            const fileDepsWithThisName = this.fileDependencies
                .filter(dep => dep.name.toLowerCase() == fileDependencyName.toLowerCase());

            if (fileDepsWithThisName.length == 0) {
                throw new Error(`file dependency ${fileDependencyName} could not be found`);
            }

            const fileDependency = fileDepsWithThisName[0];

            if (fileDependency.loadTime == "Early")
                throw new Error("This file dependency was loaded at app startup");

            if (fileDependency.loadStatus != "NotLoaded")
                return fileDependency.promise;

            fileDependency.loadStatus = "IsBeingLoaded";

            fileDependency.promise = new Promise<void>((resolve, reject) => {

                if (this.dependencyShouldBeConsidered(fileDependency) == false) {
                    reject("File dependency may not be loaded because of its predicate");
                    return;
                }

                try {

                    let element: HTMLElement = null;

                    if (fileDependency.fileDependecyType == "Script") {
                        element = document.createElement("script");
                        (element as HTMLScriptElement).type = "text/javascript";
                        (element as HTMLScriptElement).src = fileDependency.path;
                    } else {
                        element = document.createElement("link");
                        (element as HTMLLinkElement).rel = "stylesheet";
                        (element as HTMLLinkElement).type = "text/css";
                        (element as HTMLLinkElement).href = fileDependency.path;
                    }

                    fileDependency.loadStatus = "IsBeingLoaded";

                    element.onload = (): void => {

                        if (this.clientAppProfile.isDebugMode == true) {
                            console.trace(`${fileDependency.name} loaded`);
                        }

                        fileDependency.loadStatus = "Loaded";
                        resolve();

                    };

                    element.onerror = (err): void => {
                        reject(err);
                    }

                    document.head.appendChild(element);

                }
                catch (e) {
                    reject(e);
                    throw e;
                }

            });

            return fileDependency.promise;
        }

        public resolveObject<TContract>(objectDependencyName: string): TContract {

            if (objectDependencyName == null || objectDependencyName == "")
                throw new Error("argument exception: objectDependencyName");

            let result = this.resolveAllObjects<TContract>(objectDependencyName)[0];

            if (result == null) {
                for (let customObjectResolver of this.customObjectResolvers) {
                    let canResolve = false;
                    try {
                        canResolve = customObjectResolver.canResolve == null || customObjectResolver.canResolve(objectDependencyName);
                    } catch (e) { }
                    if (canResolve == true) {
                        result = customObjectResolver.resolve<TContract>(objectDependencyName);
                        if (result != null)
                            break;
                    }
                }
            }

            if (result == null) {
                throw new Error(`object dependency ${objectDependencyName} could not be found`);
            }

            return result;
        }

        public resolveAllObjects<TContract>(objectDependencyName: string): Array<TContract> {

            if (objectDependencyName == null || objectDependencyName == "")
                throw new Error("argument exception: objectDependencyName");

            const objectDepsWithThisName = this.objectDependencies
                .filter(dep => dep.name.toLowerCase() == objectDependencyName.toLowerCase());

            objectDepsWithThisName.forEach(objectDep => {

                if (objectDep.lifeCycle == "SingleInstance") {

                    const result = this.singletoneObjectDependenciesInstances
                        .filter(depInstanceKeyValue => depInstanceKeyValue.objectDep == objectDep)[0];

                    if (result == null) {
                        this.registerInstanceDependency({ instance: Reflect.construct(objectDep.type as Function, []), name: objectDep.name, overwriteExisting: false }, objectDep);
                    }

                } else {
                    throw new Error("lifeCycle not supported yet");
                }

            });

            return this.singletoneObjectDependenciesInstances
                .filter(singletoneObjDepInstance => singletoneObjDepInstance.objectDep.name.toLowerCase() == objectDependencyName.toLowerCase())
                .map(singletoneObjDepInstance => singletoneObjDepInstance.objectDepInstance) as TContract[];
        }

        public getAllDirectivesDependencies(): Array<IDirectiveDependency> {
            return this.directiveDependencies;
        }

        public getAllFormViewModelsDependencies(): Array<IFormViewModelDependency> {
            return this.formViewModelDependencies;
        }

        public getAllComponentDependencies(): Array<IComponentDependency> {
            return this.componentDependencies;
        }
    }

    export function ObjectDependency(objectDependency: IObjectDependency): ClassDecorator {

        return (targetService: IObjectDependency & Function) => {

            targetService = Injectable()(targetService) as IObjectDependency & Function;

            objectDependency.type = targetService;

            DependencyManager.getCurrent()
                .registerObjectDependency(objectDependency);

            return targetService;
        };
    }

    export function FormViewModelDependency(formViewModelDependency: IFormViewModelDependency): ClassDecorator {

        return (targetFormViewModel: IFormViewModelDependency & Function): Function => {

            targetFormViewModel = Injectable()(targetFormViewModel) as IFormViewModelDependency & Function;

            formViewModelDependency.type = targetFormViewModel;

            DependencyManager.getCurrent()
                .registerFormViewModelDependency(formViewModelDependency);

            return targetFormViewModel;
        };
    }

    export function ComponentDependency(componentDependency: IComponentDependency): ClassDecorator {

        return (targetComponent: IComponentDependency & Function): Function => {

            targetComponent = Injectable()(targetComponent) as IComponentDependency & Function;

            componentDependency.type = targetComponent;

            DependencyManager.getCurrent()
                .registerComponentDependency(componentDependency);

            return targetComponent;
        };
    }

    export function DirectiveDependency(directiveDependency: IDirectiveDependency): ClassDecorator {

        return (targetDirective: IDirectiveDependency & Function): Function => {

            targetDirective = Injectable()(targetDirective) as IDirectiveDependency & Function;

            directiveDependency.type = targetDirective;

            DependencyManager.getCurrent()
                .registerDirectiveDependency(directiveDependency);

            return targetDirective;
        };
    }

    export function Inject(name: string): ParameterDecorator {

        if (name == null || name == "")
            throw new Error("name may not be null or empty");

        return (target: Function, propertyKey: string | symbol): Function => {
            target.injects = target.injects || [];
            target.injects.push({ name: name, kind: "Single" });
            return target;
        }
    }

    export function InjectAll(name: string): ParameterDecorator {

        if (name == null || name == "")
            throw new Error("name may not be null or empty");

        return (target: Function, propertyKey: string | symbol): Function => {
            target.injects = target.injects || [];
            target.injects.push({ name: name, kind: "All" });
            return target;
        }
    }

    export function Injectable(): ClassDecorator {

        return (target: Function): Function => {

            const injects = target.injects;

            if (injects != null && injects.length != 0) {

                const originalTarget = target;

                target = function () {

                    const dependencyManager = DependencyManager.getCurrent();

                    const args = Array.from(arguments);

                    for (let inject of injects.slice(0).reverse()) {
                        if (inject.kind == "All")
                            args.push(dependencyManager.resolveAllObjects<any[]>(inject.name));
                        else
                            args.push(dependencyManager.resolveObject<any>(inject.name));
                    }

                    return Reflect.construct(originalTarget, args);
                };

            };

            return target;
        }
    }

    function camelize(str: string): string {
        return str.replace(/(?:^\w|[A-Z]|\b\w)/g, (letter, index) => (index == 0 ? letter.toLowerCase() : letter.toUpperCase())).replace(/\s+/g, "");
    }
}