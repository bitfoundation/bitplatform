module Foundation.ViewModel {

    export function Command() {

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

                            ScopeManager.update$scope($rootScope);

                        });

                        rPromise.catch((e) => {

                            ScopeManager.update$scope($rootScope);

                            const iLogger = Core.DependencyManager.getCurrent().resolveObject<Core.Contracts.ILogger>("Logger");
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }
                }
                catch (e) {

                    const iLogger = Core.DependencyManager.getCurrent().resolveObject<Core.Contracts.ILogger>("Logger");
                    iLogger.logDetailedError(this, propertyKey, args, e);

                    throw e;
                }
                finally {

                    if (isPromise == false) {

                        ScopeManager.update$scope($rootScope);

                        if (clientAppProfile.isDebugMode == true)
                            console.timeEnd(propertyKey);
                    }
                }

                return result;
            };
        };
    }
}