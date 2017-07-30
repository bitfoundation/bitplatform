module BitChangeSetManager.ViewModel.ViewModels {

    @SecureFormViewModelDependency({
        name: "ChangeSetsViewModel",
        templateUrl: `view/views/changeSetsView.html`
    })
    export class ChangeSetsViewModel extends FormViewModel {

        public changeSetsDataSource: kendo.data.DataSource;
        public deliveriesDataSource: kendo.data.DataSource;
        public customersDataSource: kendo.data.DataSource;
        public changeSetSeveritiesDataSource: kendo.data.DataSource;
        public changeSetDeliveryRequirementsDataSource: kendo.data.DataSource;

        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;
        public deliveryMetadata = BitChangeSetManagerModel.DeliveryDto;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider,
            @Inject("MessageReceiver") public messageReceiver: IMessageReceiver,
            @Inject("$mdToast") public $mdToast: ng.material.IToastService,
            @Inject("$translate") public $translate: ng.translate.ITranslateService) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");

            let [changeSetDeliveryRequirements, changeSetSeverities] = await context.batchExecuteQuery([context.changeSetDeliveryRequirements, context.changeSetSeverities]);
            this.changeSetSeveritiesDataSource = changeSetSeverities.toQueryable(BitChangeSetManagerModel.ChangeSetSeverityDto).asKendoDataSource();
            this.changeSetDeliveryRequirementsDataSource = changeSetDeliveryRequirements.toQueryable(BitChangeSetManagerModel.ChangeSetDeliveryRequirementDto).asKendoDataSource();

            this.changeSetsDataSource = context.changeSets.asKendoDataSource({ serverPaging: true, pageSize: 5, serverSorting: true, sort: { field: "Title", dir: "asc" } });

            this.deliveriesDataSource = context
                .deliveries
                .map(d => { return { Id: d.Id, CustomerName: d.CustomerName, ChangeSetId: d.ChangeSetId, DeliveredOn: d.DeliveredOn } })
                .asKendoDataSource({ serverPaging: true, pageSize: 5, serverSorting: true, sort: { field: "CustomerName", dir: "asc" } });

            this.deliveriesDataSource.asChildOf(this.changeSetsDataSource, ["ChangeSetId"], ["Id"]);

            this.customersDataSource = context
                .customers
                .map(c => { return { Id: c.Id, Name: c.Name } })
                .asKendoDataSource();

            PubSub.subscribe("ChangeSetHasBeenInsertedByUser", this.onChangeSetHasBeenInsertedByUser.bind(this));
        }

        @Command()
        public async onChangeSetHasBeenInsertedByUser(key: string, args: { userName: string, title: string }) {
            await this.$mdToast.show(this.$mdToast.simple()
                .textContent(this.$translate.instant("ChangeSetHasBeenInsertedByUser", args))
                .hideDelay(3000)
            );
        }

        public async $onDestroy() {
            PubSub.unsubscribe(this.onChangeSetHasBeenInsertedByUser);
        }
    }
}
