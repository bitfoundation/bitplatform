module Foundation.ViewModel {

    export function Command() {

        return (target: Object, propertyKey: string, descriptor: TypedPropertyDescriptor<any>) => {

            const clientAppProfile = Foundation.Core.ClientAppProfileManager.getCurrent().getClientAppProfile();

            const originalMethod = descriptor.value;

            let isPromise = false;
            let $scope: ng.IScope = null;

            descriptor.value = function (...args: any[]) {

                let result = null;

                try {

                    if (clientAppProfile.isDebugMode == true) {
                        console.time(propertyKey);
                    }

                    $scope = Foundation.Core.DependencyManager.getCurrent().resolveObject<ng.IRootScopeService>("$rootScope");

                    result = originalMethod.apply(this, args);

                    if (result instanceof Promise) {

                        isPromise = true;

                        let rPromise = result as Promise<void>;

                        rPromise.then(() => {

                            if (clientAppProfile.isDebugMode == true)
                                console.timeEnd(propertyKey);

                            ScopeManager.update$scope($scope);

                        });

                        rPromise.catch((e) => {

                            ScopeManager.update$scope($scope);

                            const iLogger = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.Core.Contracts.ILogger>("Logger");
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }
                }
                catch (e) {

                    const iLogger = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.Core.Contracts.ILogger>("Logger");
                    iLogger.logDetailedError(this, propertyKey, args, e);

                    throw e;
                }
                finally {

                    if (isPromise == false) {

                        ScopeManager.update$scope($scope);

                        if (clientAppProfile.isDebugMode == true)
                            console.timeEnd(propertyKey);
                    }
                }

                return result;
            };
        };
    }
}