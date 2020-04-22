module Bit.Tests.Implementations.Tests {
    export class UiAutomationTests {
        public static async testGetBindingContextAndGetViewModel(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.RepeatViewModel>(angular.element("#repeatView"));

            const repeatViewModel = uiAutomation.viewModel;

            if (repeatViewModel.testModels.length != 2) {
                throw new Error("problem in testGetBindingContextAndGetViewModel");
            }

            if (repeatViewModel.someProperty != "This is a view model") {
                throw new Error("problem in testGetBindingContextAndGetViewModel");
            }

            repeatViewModel.testModels.forEach((tm, i) => {

                const tmFromView = uiAutomation.getBindingContext<Bit.Tests.Model.DomainModels.TestModel>(uiAutomation.view.find(`#testModel${i}`), "tm");

                if (tmFromView.Id != tm.Id)
                    throw new Error("problem in testGetBindingContextAndGetViewModel");

            });
        }

        public static async testGettingSomeVariables(firstNum: number, secondNum: number, message: string, date: Date, obj: { firstNum: number, secondNum: number, message: string, date: Date }): Promise<void> {

            if (firstNum + secondNum != 10)
                throw new Error("problem in passing args");

            if (message != "Hi")
                throw new Error("problem in passing args");

            if (date.getFullYear() != 2016)
                throw new Error("problem in passing args");

            if (obj.date.getFullYear() != date.getFullYear())
                throw new Error("problem in passing args");

            if (obj.firstNum != firstNum)
                throw new Error("problem in passing args");

            if (obj.secondNum != secondNum)
                throw new Error("problem in passing args");

            if (obj.message != message)
                throw new Error("problem in passing args");
        }
    }
}