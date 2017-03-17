module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultDateTimeService implements Contracts.IDateTimeService {

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {
        }

        public getFormattedDate(date?: Date, culture?: string): string {

            if (DefaultDateTimeService.getFormattedDateDelegate == null)
                throw new Error("There is no implementation specified for getFormattedDate");

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? this.clientAppProfileManager.getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("YYYY/MM/DD") as string;
            }
            else {
                return DefaultDateTimeService.getFormattedDateDelegate(date);
            }

        }

        public getFormattedDateTime(date?: Date, culture?: string): string {

            if (DefaultDateTimeService.getFormattedDateTimeDelegate == null)
                throw new Error("There is no implementation specified for getFormattedDateTime");

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? this.clientAppProfileManager.getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("DD MMMM YYYY, hh:mm a") as string;
            }
            else {
                return DefaultDateTimeService.getFormattedDateTimeDelegate(date);
            }
        }

        public getCurrentDate(): Date {
            return new Date();
        }

        public parseDate(date: any): Date {

            if (DefaultDateTimeService.parseDateDelegate == null)
                throw new Error("There is no implementation specified for parseDate");

            if (date == null)
                return null;

            return DefaultDateTimeService.parseDateDelegate(date);
        }

        public static parseDateDelegate: (date: any) => Date;
        public static getFormattedDateTimeDelegate: (date: any) => string;
        public static getFormattedDateDelegate: (date: any) => string;

    }
}