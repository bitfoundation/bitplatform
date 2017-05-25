module Foundation.Test.ViewModels {
    @Core.SecureFormViewModelDependency({ name: "RadComboFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radComboview.html" })
    export class RadComboFormViewModel extends ViewModel.ViewModels.FormViewModel {

        public testModelsDataSource: kendo.data.DataSource = null;

        public model: Bit.Tests.Model.DomainModels.ParentEntity = null;

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.testModelsDataSource = context.testModels.getTestModelsByStringPropertyValue("1").asKendoDataSource();
            this.model = await context.parentEntities.getTestData().map(p => { return { Id: p.Id, TestModel: { Id: p.TestModel.Id } } }).single();
        }

        @ViewModel.Command()
        public setCurrent(): void {
            const entityToBeCurrent = this.testModelsDataSource.dataView<Bit.Tests.Model.DomainModels.TestModel>()[0];
            this.testModelsDataSource.current = entityToBeCurrent;
        }
    }
}