module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "ChangeSetsViewModel",
        template: `<rad-combo rad-datasource="vm.testsDataSource" rad-text-field-name="Id" />`
    })
    export class ChangeSetsViewModel extends FormViewModel {

        public testsDataSource: kendo.data.DataSource;

        public async $onInit(): Promise<void> {

            this.testsDataSource = [new BitChangeSetManagerModel.TestDto({ Id: 1 }), new BitChangeSetManagerModel.TestDto({ Id: 2 })]
                .toQueryable(BitChangeSetManagerModel.TestDto)
                .asKendoDataSource();

        }

    }

}