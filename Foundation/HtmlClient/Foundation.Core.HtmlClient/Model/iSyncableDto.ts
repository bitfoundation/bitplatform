module Foundation.Model.Contracts {
    export interface ISyncableDto extends IsArchivableDto {
        Id: string;
        Version: string;
        ISV: boolean;
    }
}