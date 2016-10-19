/// <reference path="../../../foundation.test.core.htmlclient/foundation.test.core.d.ts" />
module Foundation.Test.Implementations.Tests {
    export class AngularTranslateTests {

        public static async testAngularTransalateFormViewModel(): Promise<void> {

            const uiAutomation = new UIAutomation<ViewModels.AngularTranslateFormViewModel>(angular.element("#angularTranslateView"));

            await uiAutomation.formViewModel.changeText();

            if (uiAutomation.formViewModel.text != "Unknown error")
                throw new Error("problem in angular translate");

            await uiAutomation.formViewModel.changeLanguage();

            if (uiAutomation.view.find("#test2").text() != "خطای مشخص")
                throw new Error("problem in angular translate");
        }
    }
}