module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'iranianPeopleNationalCode' })
    export class IranianPeopleNationalCodeValidator implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return (): angular.IDirective => {
                return {
                    require: 'ngModel',
                    link: function ($scope: angular.IScope, elm: JQuery, attrs: angular.IAttributes, ctrl: angular.INgModelController) {

                        let dependencyManager = Core.DependencyManager.getCurrent();

                        let iranianCodeValidator = dependencyManager.resolveObject<Core.Contracts.IIranianCodeValidator>("IranianCodeValidator");

                        ctrl.$validators['iranianPeopleNationalCode'] = (modelValue: string, viewValue: string) => {

                            return iranianCodeValidator.nationalCodeIsValid(modelValue);
                        };
                    }
                };
            };
        }
    }
}