module Bit {

    export function Log() {

        return (target: Object, propertyKey: string, descriptor: TypedPropertyDescriptor<any>) => {

            const clientAppProfile = ClientAppProfileManager.getCurrent().getClientAppProfile();

            const originalMethod = descriptor.value;

            let isPromise = false;

            descriptor.value = function log(...args: any[]) {

                let result = null;

                let startTime: number = null;

                try {

                    if (clientAppProfile.isDebugMode == true) {
                        if (typeof (performance) != "undefined") {
                            startTime = performance.now();
                        } else {
                            console.time(propertyKey);
                        }
                    }

                    result = originalMethod.apply(this, args);

                    if (result instanceof Promise) {

                        isPromise = true;

                        const rPromise = result as Promise<void>;

                        rPromise.then(() => {
                            if (clientAppProfile.isDebugMode == true) {
                                if (typeof (performance) != "undefined") {
                                    console.log(`${propertyKey}: ${performance.now() - startTime}ms`);
                                } else {
                                    console.timeEnd(propertyKey);
                                }
                            }
                        });

                        rPromise.catch((e) => {

                            const iLogger = Provider.loggerProvider();
                            iLogger.logDetailedError(this, propertyKey, args, e);

                        });

                    }

                } catch (e) {

                    const iLogger = Provider.loggerProvider();
                    iLogger.logDetailedError(this, propertyKey, args, e);

                    throw e;
                }
                finally {
                    if (isPromise == false && clientAppProfile.isDebugMode == true) {
                        if (typeof (performance) != "undefined") {
                            console.log(`${propertyKey}: ${performance.now() - startTime}ms`);
                        } else {
                            console.timeEnd(propertyKey);
                        }
                    }
                }

                return result;
            };
        };
    }
}