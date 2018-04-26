module Bit.Implementations {

    export class DefaultGuidUtils {

        public newGuid(): string {

            return $data.Guid.NewGuid().value.toLowerCase();

        }

        public emptyGuid(): string {

            return "00000000-0000-0000-0000-000000000000";

        }
    }
}