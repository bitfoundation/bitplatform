module Bit.Model.Contracts {
    export type ISyncableDto = IArchivableDto & IVersionableDto & {
        IsSynced: boolean;
    };
}