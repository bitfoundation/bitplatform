module Bit.Implementations {

    export class DefaultDateTimeProvider implements Contracts.IDateTimeProvider {
        public getCurrentUtcDateTime(): Date {
            return new Date();
        }
    }
}