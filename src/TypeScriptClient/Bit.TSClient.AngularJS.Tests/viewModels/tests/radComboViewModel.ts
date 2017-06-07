module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "RadComboFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/radComboview.html" })
    export class RadComboFormViewModel extends Bit.ViewModels.FormViewModel {

        public testModelsDataSource: kendo.data.DataSource = null;

        public model: Bit.Tests.Model.DomainModels.ParentEntity = null;

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.testModelsDataSource = context.testModels.getTestModelsByStringPropertyValue("1").asKendoDataSource();
            this.model = await context.parentEntities.getTestData().map(p => { return { Id: p.Id, TestModel: { Id: p.TestModel.Id } } }).single();
        }

        @Command()
        public setCurrent(): void {
            const entityToBeCurrent = this.testModelsDataSource.dataView<Bit.Tests.Model.DomainModels.TestModel>()[0];
            this.testModelsDataSource.current = entityToBeCurrent;
        }
    }
}