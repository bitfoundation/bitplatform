module Bit.Implementations {

    export class DefaultDateTimeService implements Contracts.IDateTimeService {

        public getFormattedDate(date?: Date, culture?: string): string {

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? ClientAppProfileManager.getCurrent().getClientAppProfile().culture : culture;

            if (Provider.getFormattedDateDelegate == null)
                throw new Error("There is no implementation provided for Provider.getFormattedDateDelegate");

            return Provider.getFormattedDateDelegate(date, culture);
        }


        public getFormattedDateTime(date?: Date, culture?: string): string {

            if (date == null)
                return null;

            date = this.parseDate(date);

            culture = culture == null ? ClientAppProfileManager.getCurrent().getClientAppProfile().culture : culture;

            if (Provider.getFormattedDateTimeDelegate == null)
                throw new Error("There is no implementation provided for Provider.getFormattedDateTimeDelegate");

            return Provider.getFormattedDateTimeDelegate(date, culture);
        }

        public getCurrentDate(): Date {
            return new Date();
        }

        public parseDate(date: any): Date {

            if (date == null)
                return null;

            if (Provider.parseDateDelegate == null)
                throw new Error("There is no implementation provided for Provider.parseDateDelegate");

            return Provider.parseDateDelegate(date);
        }
    }
}