module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "ChangeSetsViewModel",
        template: `<rad-combo rad-datasource="vm.changeSetsDataSource" rad-text-field-name="Title" />`
    })
    export class ChangeSetsViewModel extends FormViewModel {

        public changeSetsDataSource: kendo.data.DataSource;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider) {
            super();
        }

        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");

            this.changeSetsDataSource = context.changeSets.asKendoDataSource();

        }

    }

}