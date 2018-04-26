module Bit.Contracts {
    export type EnvironmentCultureValue = {
        Name: string;
        Title: string;
    };

    export type EnvironmentCulture = {
        Name: string;
        Values: Array<EnvironmentCultureValue>;
    };

    export type ProjectMetadata = {
        ProjectName: string;
        Messages: Array<EnvironmentCulture>;
    };

    export type ViewMetadata = {
        ViewName: string;
        Messages: Array<EnvironmentCulture>;
    };

    export type DtoMemberMetadata = {
        IsRequired: boolean;
        Pattern: string;
        DtoMemberName: string;
        Messages: Array<EnvironmentCulture>;
    };

    export type DtoMemberLookup = {
        DtoMemberName: string;
        LookupDtoType: string;
        DataTextField: string;
        DataValueField: string;
        BaseFilter_JS: string;
    };

    export type DtoMetadata = {
        DtoType: string;
        MembersMetadata: Array<DtoMemberMetadata>;
        MembersLookups: Array<DtoMemberLookup>;
    };

    export type AppMetadata = {
        Dtos: Array<DtoMetadata>;
        Messages: Array<EnvironmentCulture>;
        Projects: Array<ProjectMetadata>;
        Views: Array<ProjectMetadata>;
    };

    export type IMetadataProvider = {
        getMetadata(): Promise<AppMetadata>;
        getMetadataSync(): AppMetadata;
    };
}