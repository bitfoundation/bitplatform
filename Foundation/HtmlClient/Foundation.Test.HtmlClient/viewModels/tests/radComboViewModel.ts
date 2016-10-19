module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RadComboFormViewModel", routeTemplate: "/rad-combo-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/radComboview.html" })
    export class RadComboFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public testModelsDataSource: kendo.data.DataSource = null;

        public model: Foundation.Test.Model.DomainModels.ParentEntity = null;

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public async $routerOnActivate(route): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            this.testModelsDataSource = context.testModels.getTestModelsByStringPropertyValue('1').asKendoDataSource();
            this.model = await context.parentEntities.getTestData().map(p => { return { Id: p.Id, TestModel: { Id: p.TestModel.Id } } }).single();
            return await super.$routerOnActivate(route);
        }

        @ViewModel.Command()
        public setCurrent(): void {
            const entityToBeCurrent = this.testModelsDataSource.dataView<Foundation.Test.Model.DomainModels.TestModel>()[0];
            this.testModelsDataSource.current = entityToBeCurrent;
        }
    }
}