module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "DateTimeServiceFormViewModel",  templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/dateTimeServiceView.html" })
    export class DateTimeServiceFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("DateTimeService") public dateTimeService: Contracts.IDateTimeService) {
            super();
        }

        public date: Date;

        @Command()
        public $onInit(): void {
            this.date = new Date(2016, 1, 1, 10, 10);
        }
    }
}