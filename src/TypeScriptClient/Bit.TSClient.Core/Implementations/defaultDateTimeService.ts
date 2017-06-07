module Bit.Implementations {

    export class DefaultDateTimeService implements Contracts.IDateTimeService {

        public getFormattedDate(date?: Date, culture?: string): string {

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? ClientAppProfileManager.getCurrent().getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("YYYY/MM/DD") as string;
            }
            else {
                return Provider.getFormattedDateDelegate(date);
            }

        }

        public getFormattedDateTime(date?: Date, culture?: string): string {

            if (Provider.getFormattedDateTimeDelegate == null)
                throw new Error("There is no implementation specified for getFormattedDateTime");

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? ClientAppProfileManager.getCurrent().getClientAppProfile().culture : culture;

            if (culture == "FaIr") {
                return persianDate(date).format("DD MMMM YYYY, hh:mm a") as string;
            }
            else {
                return Provider.getFormattedDateTimeDelegate(date);
            }
        }

        public getCurrentDate(): Date {
            return new Date();
        }

        public parseDate(date: any): Date {

            if (date == null)
                return null;

            return Provider.parseDateDelegate(date);
        }
    }
}