module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "DateTimeServiceFormViewModel",  templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/dateTimeServiceView.html" })
    export class DateTimeServiceFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("DateTimeService") public dateTimeService: ViewModel.Contracts.IDateTimeService) {
            super();
        }

        public date: Date;

        public async $routerOnActivate(route): Promise<void> {
            this.date = new Date(2016, 1, 1, 10, 10);
            return await super.$routerOnActivate(route);
        }
    }
}