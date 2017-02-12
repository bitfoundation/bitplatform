module Foundation.ViewModel {

    export function Command(configuration: { callUpdate$scope: "IfAsync" | "Always" | "Never", $appyMode: "$applyAsync" | "$apply" }
        = { callUpdate$scope: "IfAsync", $appyMode: "$applyAsync" }) {

        if (configuration == null)
            throw new Error("configuration may not be null");

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
                                ScopeManager.update$scope($rootScope, configuration.$appyMode == "$applyAsync");
                            }

                        });

                        rPromise.catch((e) => {

                            if (configuration.callUpdate$scope != "Never") {
                                ScopeManager.update$scope($rootScope, configuration.$appyMode == "$applyAsync");
                            }

                            const iLogger = Core.DependencyManager.getCurrent().resolveObject<Core.Contracts.ILogger>("Logger");
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }
                }
                finally {

                    if (isPromise == false) {

                        if (configuration.callUpdate$scope == "Always") {
                            ScopeManager.update$scope($rootScope, configuration.$appyMode == "$applyAsync");
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