module Foundation.ViewModel.ViewModels {
    export class FormViewModel {

        public async $routerCanActivate(route: any): Promise<boolean> {
            return true;
        }

        public async $routerOnActivate(route: any): Promise<void> {

        }

        public async $routerCanDeactivate(): Promise<boolean> {
            return true;
        }

        public async $routerOnDeactivate(): Promise<void> {

        }

        private _bindingContext: ng.IScope;

        public get bindingContext(): ng.IScope {
            return this._bindingContext;
        }

        public set bindingContext(value: ng.IScope) {
            this._bindingContext = value;
        }
    }
}