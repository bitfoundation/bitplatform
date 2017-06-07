module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "EntityContextUsageFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/entityContextUsageview.html" })
    export class EntityContextUsageFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super();
        }

        public num = 0;

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.num = await context.testModels.sum(10, 20);
        }
    }
}