module Foundation.Model.Contracts {
    export interface ISyncableDto extends IsArchivableDto, IVersionableDto {
        ISV: boolean;
    }
}