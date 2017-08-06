module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "RepeatFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/repeatview.html" })
    export class RepeatFormViewModel extends Bit.ViewModels.FormViewModel {

        public testModels: Array<Tests.Model.DomainModels.TestModel> = null;

        public someProperty = "This is a view model";

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getContext<TestContext>("Test");
            this.testModels = await context.testModels.getTestModelsByStringPropertyValue("1").toArray();
        }
    }
}