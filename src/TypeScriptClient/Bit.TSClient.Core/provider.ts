module Bit {

    export class Provider {

        public static loggerProvider: () => Bit.Contracts.ILogger;

        public static parseDateDelegate: (date: any) => Date;
        public static getFormattedDateTimeDelegate: (date: any) => string;
        public static getFormattedDateDelegate: (date: any) => string;

    }

}