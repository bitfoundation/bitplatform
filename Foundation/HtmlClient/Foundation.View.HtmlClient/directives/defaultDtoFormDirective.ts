module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "dtoForm" })
    export class DefaultDtoFormDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                transclude: true,
                terminal: true,
                replace: true,
                restrict: "E",
                template: ($element: JQuery, $attrs: ng.IAttributes) => {
                    let defaultNgModelOptions = `ng-model-options="{ updateOn : 'default blur' , allowInvalid : true , debounce: { 'default': 250, 'blur': 0 } }"`;
                    if ($attrs["ngModelOptions"] != null)
                        defaultNgModelOptions = "";
                    return `<ng-form ${defaultNgModelOptions} dto-form-service ng-transclude></ng-form>`;
                }
            });
        }
    }
}