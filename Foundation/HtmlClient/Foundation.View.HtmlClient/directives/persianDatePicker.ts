module Foundation.View.Directives {

    @Foundation.Core.ComponentDependency({
        name: 'persianDatePicker',
        require: {
            'ngModelController': 'ngModel'
        },
        bindings: {
            'ngModel': '=',
            'isDateTime': '<'
        },
        template: ['$element', '$attrs', ($element: JQuery, $attrs: ng.IAttributes) => {

            return `<persian-date-element>
                        <input id='main' type='text' />
                        <input type='text' id='alt' ng-model='vm.ngModel' style='display: none; '></input>
                    <persian-date-element>`;

        }]
    })
    export class PersianDateComponent {

        public constructor(public $scope: ng.IScope, public $element: JQuery, @Foundation.Core.Inject("DateTimeService") public dateTimeService: Foundation.ViewModel.Contracts.IDateTimeService) {
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

                let result = new Date(value);

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

            this.$main = this.$element.find('#main');
            this.$alt = this.$element.find('#alt');

            let isDateTime = this.isDateTime;
            let dateTimeService = this.dateTimeService;

            this.$main.pDatepicker({
                autoClose: this.isDateTime == false,
                altField: this.$alt,
                altFieldFormatter: (e) => {
                    let result = new Date(e);
                    return result
                },
                formatter: (e) => {
                    let result = new Date(e);
                    if (isDateTime == true)
                        return dateTimeService.getFormattedDateTime(result, "FaIr");
                    else
                        return dateTimeService.getFormattedDate(result, "FaIr");
                },
                timePicker: {
                    enabled: this.isDateTime == true
                }
            });
        }
    }

    PersianDateComponent.$inject = ['$scope', '$element'];
}