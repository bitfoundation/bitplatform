module Bit.Tests.ViewModels {

    @ComponentDependency({ name: "AsyncViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/asyncview.html" })
    export class AsyncViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            
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