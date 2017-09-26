module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "DateTimeServiceViewModel",  templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/dateTimeServiceView.html" })
    export class DateTimeServiceViewModel {

        public constructor( @Inject("DateTimeService") public dateTimeService: Contracts.IDateTimeService) {
            
        }

        public date: Date;

        @Command()
        public $onInit(): void {
            this.date = new Date(2016, 1, 1, 10, 10);
        }
    }
}