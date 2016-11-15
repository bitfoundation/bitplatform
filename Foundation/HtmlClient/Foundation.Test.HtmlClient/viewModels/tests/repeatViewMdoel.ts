module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RepeatFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/repeatview.html" })
    export class RepeatFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public testModels: Array<Model.DomainModels.TestModel> = null;

        public someProperty = "This is a view model";

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        @Foundation.ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.testModels = await context.testModels.getTestModelsByStringPropertyValue('1').toArray();
        }
    }
}