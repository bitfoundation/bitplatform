module Bit.Model.Contracts {
    export type IArchivableDto = IDto & {
        IsArchived: boolean;
    };
}