module Foundation.Model.Contracts {
    export interface ISyncableDto extends IArchivableDto, IVersionableDto {
        ISV: boolean;
    }
}