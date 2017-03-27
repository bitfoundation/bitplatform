module Foundation.ViewModel {

    export class Default$scopeConfiguration {
        public static currentConfig: { callUpdate$scope?: "IfAsync" | "Always" | "Never", $appyMode?: "$applyAsync" | "$apply" } = { callUpdate$scope: "IfAsync", $appyMode: "$applyAsync" };
    }

    export function Command(configuration = Default$scopeConfiguration.currentConfig) {

        if (configuration == null)
            throw new Error("configuration may not be null");

        if (configuration.$appyMode == null)
            configuration.$appyMode = Default$scopeConfiguration.currentConfig.$appyMode;

        if (configuration.callUpdate$scope == null)
            configuration.callUpdate$scope = Default$scopeConfiguration.currentConfig.callUpdate$scope;

        return (target: Object, propertyKey: string, descriptor: TypedPropertyDescriptor<any>) => {

            const clientAppProfile = Core.ClientAppProfileManager.getCurrent().getClientAppProfile();

            const originalMethod = descriptor.value;

            let isPromise = false;
            let $rootScope: ng.IScope = null;

            descriptor.value = function (...args: any[]) {

                let result = null;

                try {

                    if (clientAppProfile.isDebugMode == true) {
                        console.time(propertyKey);
                    }

                    $rootScope = Core.DependencyManager.getCurrent().resolveObject<ng.IRootScopeService>("$rootScope");

                    result = originalMethod.apply(this, args);

                    if (result instanceof Promise) {

                        isPromise = true;

                        const rPromise = result as Promise<void>;

                        rPromise.then(() => {

                            if (clientAppProfile.isDebugMode == true)
                                console.timeEnd(propertyKey);

                            if (configuration.callUpdate$scope != "Never") {
                                ScopeManager.update$scope($rootScope, configuration.$appyMode);
                            }

                        });

                        rPromise.catch((e) => {

                            if (configuration.callUpdate$scope != "Never") {
                                ScopeManager.update$scope($rootScope, configuration.$appyMode);
                            }

                            const iLogger = Core.DependencyManager.getCurrent().resolveObject<Core.Contracts.ILogger>("Logger");
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }
                }
                finally {

                    if (isPromise == false) {

                        if (configuration.callUpdate$scope == "Always") {
                            ScopeManager.update$scope($rootScope, configuration.$appyMode);
                        }

                        if (clientAppProfile.isDebugMode == true)
                            console.timeEnd(propertyKey);
                    }
                }

                return result;
            };
        };
    }
}