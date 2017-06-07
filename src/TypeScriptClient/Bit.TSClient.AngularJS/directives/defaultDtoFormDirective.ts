module Bit.Directives {

    @DirectiveDependency({
        name: "DtoForm",
        scope: false,
        bindToController: {

        },
        require: {
            ngModel: "ngModel"
        },
        transclude: true,
        terminal: true,
        replace: true,
        restrict: "E",
        template: ($element: JQuery, $attrs: ng.IAttributes & { ngModelOptions: string }) => {
            let defaultNgModelOptions = `ng-model-options="{ updateOn : 'default blur' , allowInvalid : true , debounce: { 'default': 250, 'blur': 0 } }"`;
            return `<ng-form ${$attrs.ngModelOptions || defaultNgModelOptions} dto-form-service ng-transclude></ng-form>`;
        }
    })
    export class DefaultDtoFormDirective {

    }
}