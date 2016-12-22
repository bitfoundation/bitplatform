/// <reference path="../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />
let dateRegex = /(\d{4})-(\d{2})-(\d{2})T(\d{2})\:(\d{2})\:(\d{2})[+-](\d{2})\:(\d{2})/

let isDate = (value) => {
    return value instanceof Date || dateRegex.test(value);
};

let objectResolver = (key, value) => {
    if (isDate(value)) {
        return new Date(value);
    }
    return value;
}

let executeTest = async (testFunc: Function, args: string | any): Promise<void> => {

    if (testFunc == null)
        throw new Error("testFunc is null");

    try {

        Foundation.ViewModel.ScopeManager.Use$ApplyAsync = false;

        const jasmineReq = jasmineRequire;

        window["jasmine"] = jasmineReq.core(jasmineReq);

        const env = jasmine.getEnv();

        const jasmineInterface = jasmineReq.interface(jasmine, env);

        const extend = (destination, source) => {
            for (let property in source)
                destination[property] = source[property];
            return destination;
        };

        extend(window, jasmineInterface);

        jasmine.DEFAULT_TIMEOUT_INTERVAL = 10000;

        await Foundation.Core.DependencyManager.getCurrent().resolveFile("jasmine-jquery");

        let itResult = null;

        const waitHandle = new Promise<void>((resolve, reject) => {

            describe("Describe", () => {
                itResult = it("It", async (done) => {
                    try {
                        await testFunc.apply(this, typeof args == "string" ? JSON.parse(args, objectResolver) : args);
                        angular.element("#testsConsole").val("Success");
                        resolve();
                    }
                    catch (e) {
                        reject(e);
                        throw e;
                    }
                    finally {
                        done();
                    }
                });
            });

            env.execute();
        });

        await waitHandle;

        if (itResult.result.failedExpectations.length != 0) {
            let errorMessage = "";
            for (let failedExpIndex = 0; failedExpIndex < itResult.result.failedExpectations.length; failedExpIndex++) {
                errorMessage += "\n" + itResult.result.failedExpectations[failedExpIndex].message;
            }
            throw new Error(errorMessage);
        }
    }
    catch (e) {
        console.error(e);
        angular.element("#testsConsole").val(e.message);
    }
}