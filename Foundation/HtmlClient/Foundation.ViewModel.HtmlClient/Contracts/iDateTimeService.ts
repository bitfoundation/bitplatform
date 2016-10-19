module Foundation.ViewModel.Contracts {
    export interface IDateTimeService {
        getFormattedDate(date: Date): string;
        getFormattedDateTime(date: Date): string;
        getCurrentDate(): Date;
    }
}