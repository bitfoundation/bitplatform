module Foundation.ViewModel.Implementations {
    export class DefaultDateTimeService implements Contracts.IDateTimeService {

        public constructor() {

            let originalParseDate = kendo.parseDate;

            kendo.parseDate = function (value: string, format?: string, culture?: string): Date {
                if (value != null) {
                    let _date = new Date(value);
                    if (_date.toString() != 'Invalid Date')
                        return _date;
                }
                return originalParseDate.apply(this, arguments);
            };

        }

        public getFormattedDate(date?: Date): string {
            if (date == null)
                return null;
            if (Core.ClientAppProfileManager.getCurrent().clientAppProfile.culture == "FaIr") {
                return persianDate(date).format('YYYY/MM/DD') as string;
            }
            else {
                return kendo.toString(date, "yyyy/dd/MM");
            }
        }

        public getFormattedDateTime(date?: Date): string {
            if (date == null)
                return null;
            if (Core.ClientAppProfileManager.getCurrent().clientAppProfile.culture == "FaIr") {
                return persianDate(date).format('DD MMMM YYYY, hh:mm a') as string;
            }
            else {
                return kendo.toString(date, "yyyy/dd/MM, hh:mm tt");
            }
        }

        public getCurrentDate(): Date {
            return new Date();
        }

    }
}