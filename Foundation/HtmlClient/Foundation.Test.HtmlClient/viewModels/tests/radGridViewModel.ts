module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RadGridFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radGridview.html" })
    export class RadGridFormViewModel extends ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider, @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        public parentEntitiesDataSource: kendo.data.DataSource = null;
        public parentEntitiesDataSourceForFilter: kendo.data.DataSource = null;

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
    }
}