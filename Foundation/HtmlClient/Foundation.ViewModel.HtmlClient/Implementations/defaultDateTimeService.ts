module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultDateTimeService implements Contracts.IDateTimeService {

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {
        }

        public getFormattedDate(date?: Date, culture?: string): string {

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? this.clientAppProfileManager.getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("YYYY/MM/DD") as string;
            }
            else {
                return kendo.toString(date, "yyyy/dd/MM");
            }

        }

        public getFormattedDateTime(date?: Date, culture?: string): string {

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? this.clientAppProfileManager.getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("DD MMMM YYYY, hh:mm a") as string;
            }
            else {
                return kendo.toString(date, "yyyy/dd/MM, hh:mm tt");
            }
        }

        public getCurrentDate(): Date {
            return new Date();
        }

        public parseDate(date: any): Date {
            if (date == null)
                return null;
            return kendo.parseDate(date);
        }

    }
}