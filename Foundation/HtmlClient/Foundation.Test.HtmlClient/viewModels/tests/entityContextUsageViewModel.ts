module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "EntityContextUsageFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/entityContextUsageview.html" })
    export class EntityContextUsageFormViewModel extends ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public num = 0;

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.num = await context.testModels.sum(10, 20);
        }
    }
}