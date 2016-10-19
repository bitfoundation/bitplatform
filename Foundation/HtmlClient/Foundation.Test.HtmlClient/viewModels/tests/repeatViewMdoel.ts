module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RepeatFormViewModel", routeTemplate: "/repeat-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/repeatview.html" })
    export class RepeatFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public testModels: Array<Model.DomainModels.TestModel> = null;

        public someProperty = "This is a view model";

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public async $routerOnActivate(route): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            this.testModels = await context.testModels.getTestModelsByStringPropertyValue('1').toArray();
            return await super.$routerOnActivate(route);
        }
    }
}