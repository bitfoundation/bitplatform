module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "EntityContextUsageViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/entityContextUsageview.html" })
    export class EntityContextUsageViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            
        }

        public num = 0;

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.num = await context.testModels.sum(10, 20);
        }
    }
}