module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "EntityContextUsageFormViewModel", routeTemplate: "/entity-context-usage-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/entityContextUsageview.html" })
    export class EntityContextUsageFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public num = 0;

        public async $routerOnActivate(route): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            this.num = await context.testModels.sum(10, 20);
            return await super.$routerOnActivate(route);
        }
    }
}