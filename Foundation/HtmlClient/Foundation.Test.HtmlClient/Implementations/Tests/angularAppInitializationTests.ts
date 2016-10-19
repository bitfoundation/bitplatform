module Foundation.Test.Implementations.Tests {
    export class AngularAppInitializationTests {

        public static async testAsyncFormViewModel(): Promise<void> {
            const uiAutomation = new UIAutomation<ViewModels.AsyncFormViewModel>(angular.element("#asyncView"));
            await uiAutomation.formViewModel.runSumAsync();
            if (uiAutomation.formViewModel.sumResult != 30)
                throw new Error("async view model problem");
        }
    }
}