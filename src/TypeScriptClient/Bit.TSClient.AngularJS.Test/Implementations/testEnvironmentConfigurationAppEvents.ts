module Bit.Implementations {
    @ObjectDependency({ name: "AppEvent" })
    export class TestEnvironmentConfigurationAppEvents implements Contracts.IAppEvents {
        public async onAppStartup(): Promise<void> {
            angular.element(document.body).ready(() => {
                angular.element(document.body).append("<input type='text' style='visibility:collapse' id='testsConsole'>").val("Test environment is ready");
            });
        }
    }
}