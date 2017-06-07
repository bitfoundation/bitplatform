module Bit.Tests.ViewModels {

    @SecureFormViewModelDependency({ name: "AsyncFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/asyncview.html" })
    export class AsyncFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super();
        }

        public sumResult: number = null;
        private context: TestContext;

        @Command()
        public async $onInit(): Promise<void> {
            this.context = await this.entityContextProvider.getContext<TestContext>("Test");
        }

        @Command()
        public async runSumAsync(): Promise<void> {
            this.sumResult = await this.context.testModels.sum(10, 20);
        }
    }
}