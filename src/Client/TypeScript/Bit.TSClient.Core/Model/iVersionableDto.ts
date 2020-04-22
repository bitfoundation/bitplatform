module Bit.Model.Contracts {
    export type IVersionableDto = IDto & {
        Version: string;
    };
}