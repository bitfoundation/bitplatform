module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RadComboFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radComboview.html" })
    export class RadComboFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public testModelsDataSource: kendo.data.DataSource = null;

        public model: Foundation.Test.Model.DomainModels.ParentEntity = null;

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        @Foundation.ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            this.testModelsDataSource = context.testModels.getTestModelsByStringPropertyValue('1').asKendoDataSource();
            this.model = await context.parentEntities.getTestData().map(p => { return { Id: p.Id, TestModel: { Id: p.TestModel.Id } } }).single();
        }

        @ViewModel.Command()
        public setCurrent(): void {
            const entityToBeCurrent = this.testModelsDataSource.dataView<Foundation.Test.Model.DomainModels.TestModel>()[0];
            this.testModelsDataSource.current = entityToBeCurrent;
        }
    }
}