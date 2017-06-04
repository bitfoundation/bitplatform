module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "iranianPeopleNationalCode", usesOldStyle: true })
    export class IranianPeopleNationalCodeValidator implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return (): ng.IDirective => {
                return {
                    require: "ngModel",
                    link($scope: ng.IScope, elm: JQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) {

                        const dependencyManager = Core.DependencyManager.getCurrent();

                        const iranianCodeValidator = dependencyManager.resolveObject<Core.Contracts.IIranianCodeValidator>("IranianCodeValidator");

                        ctrl.$validators["iranianPeopleNationalCode"] = (modelValue: string, viewValue: string) => {

                            return iranianCodeValidator.nationalCodeIsValid(modelValue);
                        };
                    }
                };
            };
        }
    }
}