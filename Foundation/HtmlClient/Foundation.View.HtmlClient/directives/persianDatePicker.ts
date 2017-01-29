module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "ngUpdateHidden" }) // http://stackoverflow.com/a/24241282
    export class NgUpdateHidden implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return (): ng.IDirective => {
                return {
                    restrict: 'A',
                    scope: {},
                    replace: true,
                    require: 'ngModel',
                    link: function ($scope: ng.IScope, elem: JQuery, attr: ng.IAttributes, ngModel: ng.INgModelController & string) {

                        let dateTimeService = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IDateTimeService>("DateTimeService");

                        elem.change(() => {
                            $scope.$applyAsync(() => {
                                let val = elem.val();
                                if (val == null || (dateTimeService.parseDate(val) instanceof Date) == true) {
                                    ngModel.$setViewValue(val);
                                }
                            });
                        });
                    }
                };
            };
        }
    }

    @Core.ComponentDependency({
        name: "persianDatePicker",
        require: {
            'ngModelController': "ngModel"
        },
        bindings: {
            'ngModel': "=",
            'isDateTime': "<"
        },
        template: ["$element", "$attrs", ($element: JQuery, $attrs: ng.IAttributes) => {

            return `<persian-date-element>
                        <input id='main' type='text' />
                        <input id='alt' ng-model='vm.ngModel' type='hidden' ng-update-hidden ></input>
                    <persian-date-element>`;

        }]
    })
    export class PersianDateComponent {

        public constructor(public $scope: ng.IScope, public $element: JQuery, @Core.Inject("DateTimeService") public dateTimeService: ViewModel.Contracts.IDateTimeService) {
        }

        public isDateTime: boolean;
        public ngModel: Date;
        public ngModelController: ng.INgModelController;
        public $main: JQuery;
        public $alt: JQuery;

        public async $onInit(): Promise<void> {

            this.isDateTime = this.isDateTime || false;

            this.ngModelController.$formatters = this.ngModelController.$formatters || [];

            this.ngModelController.$formatters.push((value: Date): string => {

                const result = new Date(value);

                let formattedResult: string;

                if (this.isDateTime == true)
                    formattedResult = this.dateTimeService.getFormattedDateTime(result, "FaIr");
                else
                    formattedResult = this.dateTimeService.getFormattedDate(result, "FaIr");

                if (this.$alt != null)
                    this.$alt.val(result.toString());

                if (this.$main != null)
                    this.$main.val(formattedResult);

                return formattedResult;

            });
        }

        public async $postLink(): Promise<void> {

            this.$main = this.$element.find("#main");
            this.$alt = this.$element.find("#alt");

            this.$main.change(e => { // Clears ngModel when user clears text box
                if (this.$main.val() == null || this.$main.val() == "")
                    this.ngModel = undefined;
            });

            this.$main.focus(e => {
                if (this.ngModelController.$isEmpty(this.ngModel)) {
                    this.$main.val("");
                }
            });

            const isDateTime = this.isDateTime;
            const dateTimeService = this.dateTimeService;

            this.$main.pDatepicker({
                autoClose: this.isDateTime == false,
                altField: this.$alt,
                altFieldFormatter: (e) => {
                    const result = new Date(e);
                    return result;
                },
                formatter: (e) => {
                    const result = new Date(e);
                    if (isDateTime == true)
                        return dateTimeService.getFormattedDateTime(result, "FaIr");
                    else
                        return dateTimeService.getFormattedDate(result, "FaIr");
                },
                timePicker: {
                    enabled: this.isDateTime == true
                }
            });

            if (typeof ngMaterial != "undefined") {

                const mdInputContainerParent = this.$main.parents("md-input-container");

                if (mdInputContainerParent.length != 0) {

                    if (!this.ngModelController.$isEmpty(this.ngModel)) {
                        mdInputContainerParent.addClass("md-input-has-value");
                    }

                }
            }

            if (this.ngModelController.$isEmpty(this.ngModel)) {
                this.ngModel = undefined;
            }
        }
    }

    PersianDateComponent.$inject = ["$scope", "$element"];
}