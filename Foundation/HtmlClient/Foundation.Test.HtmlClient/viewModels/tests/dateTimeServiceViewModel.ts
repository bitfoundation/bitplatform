module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "DateTimeServiceFormViewModel",  templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/dateTimeServiceView.html" })
    export class DateTimeServiceFormViewModel extends ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("DateTimeService") public dateTimeService: ViewModel.Contracts.IDateTimeService) {
            super();
        }

        public date: Date;

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            this.date = new Date(2016, 1, 1, 10, 10);
        }
    }
}