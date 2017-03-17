module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "ChangeSetsViewModel",
        templateUrl: `view/changeSetsView.html`
    })
    export class ChangeSetsViewModel extends SecureViewModel {

        public changeSetsDataSource: kendo.data.DataSource;
        public deliveriesDataSource: kendo.data.DataSource;
        public customersDataSource: kendo.data.DataSource;

        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;
        public deliveryMetadata = BitChangeSetManagerModel.DeliveryDto;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");

            this.changeSetsDataSource = context.changeSets.asKendoDataSource({ serverPaging: true, pageSize: 5 });

            this.deliveriesDataSource = context
                .deliveries
                .map(d => { return { Id: d.Id, CustomerName: d.CustomerName, ChangeSetId: d.ChangeSetId, DeliveredOn: d.DeliveredOn } })
                .asKendoDataSource({ serverPaging: true, pageSize: 5 });

            this.deliveriesDataSource.asChildOf(this.changeSetsDataSource, ["ChangeSetId"], ["Id"]);

            this.customersDataSource = context
                .customers
                .map(c => { return { Id: c.Id, Name: c.Name } })
                .asKendoDataSource();
        }
    }
}