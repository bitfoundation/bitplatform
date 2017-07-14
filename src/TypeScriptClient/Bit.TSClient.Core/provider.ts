module Bit {

    export class Provider {

        public static loggerProvider: () => Bit.Contracts.ILogger;

        public static getFormattedDateDelegate: (date: Date, culture: string) => string;
        public static getFormattedDateTimeDelegate: (date: Date, culture: string) => string;
        public static parseDateDelegate: (date: any) => Date;

    }

}