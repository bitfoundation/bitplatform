module Bit.Tests.Implementations.Tests {

    export class AngularTranslateTests {

        public static async testAngularTranslateViewModel(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.AngularTranslateViewModel>(angular.element("#angularTranslateView"));

            await uiAutomation.viewModel.changeText();

            if (uiAutomation.viewModel.text != "Unknown error")
                throw new Error("problem in angular translate");

            await uiAutomation.viewModel.changeLanguage();

            if (uiAutomation.view.find("#test2").text() != "خطای مشخص")
                throw new Error("problem in angular translate");
        }
    }
}
