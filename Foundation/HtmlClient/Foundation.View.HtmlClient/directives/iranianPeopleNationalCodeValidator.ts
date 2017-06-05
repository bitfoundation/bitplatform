module Foundation.View.Directives {

    @Core.DirectiveDependency({
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

        public constructor( @Core.Inject("$element") public $element: JQuery, @Core.Inject("IranianCodeValidator") public iranianCodeValidator: Core.Contracts.IIranianCodeValidator) {

        }

        public ngModel: ng.INgModelController;

        public $onInit() {

            this.ngModel.$validators["iranian-people-national-code"] = (modelValue: string, viewValue: string) => {
                return this.iranianCodeValidator.nationalCodeIsValid(modelValue);
            };

        }
    }
}