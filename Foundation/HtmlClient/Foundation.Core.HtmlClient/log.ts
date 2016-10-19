module Foundation.Core {

    export function Log() {

        return (target: Object, propertyKey: string, descriptor: TypedPropertyDescriptor<any>) => {

            const clientAppProfile = ClientAppProfileManager.getCurrent().getClientAppProfile();

            const originalMethod = descriptor.value;

            let isPromise = false;

            descriptor.value = function (...args: any[]) {

                let result = null;

                try {

                    if (clientAppProfile.isDebugMode == true) {
                        console.time(propertyKey);
                    }

                    result = originalMethod.apply(this, args);

                    if (result instanceof Promise) {

                        isPromise = true;

                        let rPromise = result as Promise<void>;

                        rPromise.then(() => {
                            if (clientAppProfile.isDebugMode == true)
                                console.timeEnd(propertyKey);
                        });

                        rPromise.catch((e) => {

                            const iLogger = DependencyManager.getCurrent().resolveObject<Contracts.ILogger>("Logger");
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }

                }
                catch (e) {

                    const iLogger = DependencyManager.getCurrent().resolveObject<Contracts.ILogger>("Logger");
                    iLogger.logDetailedError(this, propertyKey, args, e);

                    throw e;
                }
                finally {
                    if (isPromise == false && clientAppProfile.isDebugMode == true)
                        console.timeEnd(propertyKey);
                }

                return result;
            };
        };
    }
}