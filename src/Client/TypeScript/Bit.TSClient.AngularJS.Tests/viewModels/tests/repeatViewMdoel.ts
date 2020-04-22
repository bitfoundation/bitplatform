module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "RepeatViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/repeatview.html" })
    export class RepeatViewModel {

        public testModels: Array<Tests.Model.DomainModels.TestModel> = null;

        public someProperty = "This is a view model";

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
        }

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.testModels = await context.testModels.getTestModelsByStringPropertyValue("1").toArray();
        }
    }
}