module Foundation.Test.ViewModels {

    @Core.FormViewModelDependency({ name: "AsyncFormViewModel", routeTemplate: "/async-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/asyncview.html" })
    export class AsyncFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public sumResult: number = null;
        private context: TestContainer;

        public async $routerOnActivate(route): Promise<void> {
            this.context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            return await super.$routerOnActivate(route);
        }

        @ViewModel.Command()
        public async runSumAsync(): Promise<void> {
            this.sumResult = await this.context.testModels.sum(10, 20);
        }
    }
}