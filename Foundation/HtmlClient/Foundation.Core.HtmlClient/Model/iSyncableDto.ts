module Foundation.Model.Contracts {
    export interface ISyncableDto extends IsArchivableDto {
        Version: string;
        ISV: boolean;
    }
}