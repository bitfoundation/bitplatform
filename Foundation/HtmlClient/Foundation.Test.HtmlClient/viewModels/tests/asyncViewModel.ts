module Foundation.Test.ViewModels {

    @Core.FormViewModelDependency({ name: "AsyncFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/asyncview.html" })
    export class AsyncFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public sumResult: number = null;
        private context: TestContext;

        @Foundation.ViewModel.Command()
        public async $onInit(): Promise<void> {
            this.context = await this.entityContextProvider.getReadContext<TestContext>("Test");
        }

        @ViewModel.Command()
        public async runSumAsync(): Promise<void> {
            this.sumResult = await this.context.testModels.sum(10, 20);
        }
    }
}