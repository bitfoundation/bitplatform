module Bit.Tests.Implementations.Tests {
    export class RadTests {

        public static async testRadGridViewModelAdd(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.RadGridViewModel>(angular.element("#radGridView"));

            const vm = uiAutomation.viewModel;

            const grid = uiAutomation.view.find("#gridView").data("kendoGrid");

            await uiAutomation.delay();

            await grid.addRow();

            const parentEntity = vm.parentEntitiesDataSource.current as Bit.Tests.Model.DomainModels.ParentEntity;

            parentEntity.Name = "!";

            grid.saveRow();

            await uiAutomation.delay();

            if (parentEntity.Id != "999") {
                throw new Error("rad grid add problem");
            }

            if (parentEntity.Name != "!") {
                throw new Error("rad grid add problem");
            }

            if (angular.element("#gridView").find("td").filter((tdIndex: number, tdElement: HTMLTableDataCellElement) => tdElement.innerText.trim() == "999").length == 0) {
                throw new Error("rad grid add problem");
            }
        }

        public static async testRadComboViewModel(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.RadComboViewModel>(angular.element("#radComboView"));

            uiAutomation.viewModel.testModelsDataSource.current = uiAutomation.viewModel.testModelsDataSource.dataView<Bit.Tests.Model.DomainModels.TestModel>().find(i => i["StringProperty"] == "String2");

            const vm = uiAutomation.viewModel;

            uiAutomation.updateUI();

            if (vm.model.TestModel.Id != "2")
                throw new Error("rad combo problem");

            if (uiAutomation.view.find("#test2").text() != "2")
                throw new Error("rad combo problem");

            if (uiAutomation.view.find("#test3").text() != "String2")
                throw new Error("rad combo problem");

            await uiAutomation.viewModel.setCurrent();

            if (vm.model.TestModel.Id as any != "1") // https://github.com/Microsoft/TypeScript/issues/29965
                throw new Error("rad combo problem");

            if (uiAutomation.view.find("#test2").text() != "1")
                throw new Error("rad combo problem");

            if (uiAutomation.view.find("#test3").text() != "String1")
                throw new Error("rad combo problem");
        }
    }
}