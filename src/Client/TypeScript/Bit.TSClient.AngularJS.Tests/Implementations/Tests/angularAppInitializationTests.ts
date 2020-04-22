module Bit.Tests.Implementations.Tests {
    export class AngularAppInitializationTests {

        public static async testAsyncViewModel(): Promise<void> {
            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.AsyncViewModel>(angular.element("#asyncView"));
            await uiAutomation.viewModel.runSumAsync();
            if (uiAutomation.viewModel.sumResult != 30)
                throw new Error("async view model problem");
        }
    }
}