module Bit.Directives {

    @DirectiveDependency({
        name: "IranianPeopleNationalCode",
        require: {
            ngModel: "ngModel"
        },
        restrict: "A",
        bindToController: {
        },
        scope: {}
    })
    export class IranianPeopleNationalCodeValidator {

        public constructor( @Inject("$element") public $element: JQuery, @Inject("IranianCodeValidator") public iranianCodeValidator: Contracts.IIranianCodeValidator) {

        }

        public ngModel: ng.INgModelController;

        public $onInit() {

            this.ngModel.$validators["iranian-people-national-code"] = (modelValue: string, viewValue: string) => {
                return this.iranianCodeValidator.nationalCodeIsValid(modelValue);
            };

        }
    }
}