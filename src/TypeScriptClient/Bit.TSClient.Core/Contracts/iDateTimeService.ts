module Bit.Contracts {
    export interface IDateTimeService {
        getFormattedDate(date: Date, culture?: string): string;
        getFormattedDateTime(date: Date, culture?: string): string;
        getCurrentDate(): Date;
        parseDate(date: any): Date;
    }
}