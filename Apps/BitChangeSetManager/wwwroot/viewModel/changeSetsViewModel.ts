module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "ChangeSetsViewModel",
        templateUrl: `view/changeSetsView.html`
    })
    export class ChangeSetsViewModel extends SecureViewModel {

        public changeSetsDataSource: kendo.data.DataSource;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider) {
            super();
        }

        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;

        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");

            this.changeSetsDataSource = context.changeSets.getAllChangeSets().asKendoDataSource({ serverPaging: true, pageSize: 5 });

        }

    }

}