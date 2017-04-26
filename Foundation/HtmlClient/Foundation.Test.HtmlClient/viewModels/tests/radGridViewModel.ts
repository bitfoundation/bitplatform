module Foundation.Test.ViewModels {
    @Core.SecureFormViewModelDependency({ name: "RadGridFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radGridview.html" })
    export class RadGridFormViewModel extends ViewModel.ViewModels.FormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider, @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        public parentEntitiesDataSource: kendo.data.DataSource = null;
        public parentEntitiesDataSourceForFilter: kendo.data.DataSource = null;
        public parentEntityMetadata = Model.DomainModels.ParentEntity;

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.parentEntitiesDataSource = context.parentEntities.asKendoDataSource({ pageSize: 5 });
            this.parentEntitiesDataSourceForFilter = context.parentEntities.asKendoDataSource();
        }

        @ViewModel.Command()
        public doSomethingWithCurrentEntity() {
            const parentEntity = this.parentEntitiesDataSource.current as Model.DomainModels.ParentEntity;
            parentEntity.Name += "?";
            this.$document.attr("title", parentEntity.Name);
        }

        @ViewModel.Command()
        public onDetailInit(data: Model.DomainModels.ParentEntity): void {

            console.warn(data.Id);

        }

        @ViewModel.Command()
        public onSort(kendoEvent: kendo.ui.GridSortEvent) {

        }
    }
}