/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    @Core.ObjectDependency({ name: "AppEvent" })
    export class TestEnvironmentConfigurationAppEvents implements Core.Contracts.IAppEvents {
        public async onAppStartup(): Promise<void> {
            angular.element(document.body).ready(() => {
                angular.element(document.body).append("<input type='text' style='visibility:collapse' id='testsConsole'>").val("Test environment is ready");
            });
        }
    }
}