module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "IranianPeopleNationalCode",
        require: "ngModel"
    })
    export class IranianPeopleNationalCodeValidator {

        public constructor( @Core.Inject("$element") public $element: JQuery, @Core.Inject("IranianCodeValidator") public iranianCodeValidator: Core.Contracts.IIranianCodeValidator) {

        }

        public $onInit() {

            let ngModel = this.$element.data('$ngModelController');

            ngModel.$validators["iranian-people-national-code"] = (modelValue: string, viewValue: string) => {
                return this.iranianCodeValidator.nationalCodeIsValid(modelValue);
            };

        }
    }
}