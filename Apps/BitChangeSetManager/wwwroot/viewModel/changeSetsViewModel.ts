module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "ChangeSetsViewModel",
        templateUrl: `view/changeSetsView.html`
    })
    export class ChangeSetsViewModel extends SecureViewModel {

        public changeSetsDataSource: kendo.data.DataSource;
        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider) {
            super();
        }

        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");

            this.changeSetsDataSource = context.changeSets.asKendoDataSource({ serverPaging: true, pageSize: 5 });

        }

    }

}