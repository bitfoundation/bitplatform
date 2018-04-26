module Bit.Contracts {
    export interface IDateTimeProvider {
        getCurrentUtcDateTime(): Date;
    }
}