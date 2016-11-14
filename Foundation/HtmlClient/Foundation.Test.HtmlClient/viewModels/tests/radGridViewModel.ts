module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RadGridFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radGridview.html" })
    export class RadGridFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider, @Core.Inject("$document") public $document: angular.IDocumentService) {
            super();
        }

        public parentEntitiesDataSource: kendo.data.DataSource = null;

        @Foundation.ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContext>("Test");
            this.parentEntitiesDataSource = context.parentEntities.asKendoDataSource({ pageSize: 5 });
        }

        @Foundation.ViewModel.Command()
        public doSomethingWithCurrentEntity() {
            let parentEntity = this.parentEntitiesDataSource.current as Model.DomainModels.ParentEntity;
            parentEntity.Name += "?";
            this.$document.attr('title', parentEntity.Name);
        }
    }
}