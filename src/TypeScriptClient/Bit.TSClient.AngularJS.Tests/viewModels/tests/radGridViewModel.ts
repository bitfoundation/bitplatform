module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "RadGridViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/radGridview.html" })
    export class RadGridViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider, @Inject("$document") public $document: ng.IDocumentService) {

        }

        public parentEntitiesDataSource: kendo.data.DataSource = null;
        public parentEntitiesDataSourceForFilter: kendo.data.DataSource = null;
        public parentEntityMetadata = Tests.Model.DomainModels.ParentEntity;

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.parentEntitiesDataSource = context.parentEntities.asKendoDataSource({ pageSize: 5 });
            this.parentEntitiesDataSourceForFilter = context.parentEntities.asKendoDataSource();
        }

        @Command()
        public doSomethingWithCurrentEntity() {
            const parentEntity = this.parentEntitiesDataSource.current as Tests.Model.DomainModels.ParentEntity;
            parentEntity.Name += "?";
            this.$document.attr("title", parentEntity.Name);
        }

        @Command()
        public onDetailInit(data: Tests.Model.DomainModels.ParentEntity): void {

            console.warn(data.Id);

        }

        @Command()
        public onSort(kendoEvent: kendo.ui.GridSortEvent) {

        }
    }
}