module Bit.Model.Contracts {
    export interface ISyncableDto extends IArchivableDto, IVersionableDto {
        IsSynced: boolean;
    }
}