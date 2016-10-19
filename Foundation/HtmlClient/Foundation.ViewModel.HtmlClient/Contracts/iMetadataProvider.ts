module Foundation.ViewModel.Contracts {
    export interface EnvironmentCultureValue {
        Name: string;
        Title: string;
    }

    export interface EnvironmentCulture {
        Name: string;
        Values: Array<EnvironmentCultureValue>;
    }

    export interface ProjectMetadata {
        ProjectName: string;
        Messages: Array<EnvironmentCulture>;
    }

    export interface ViewMetadata {
        ViewName: string;
        Messages: Array<EnvironmentCulture>;
    }

    export interface DtoMemberMetadata {
        IsRequired: boolean;
        Pattern: string;
        DtoMemberName: string;
        Messages: Array<EnvironmentCulture>;
    }

    export interface DtoMetadata {
        DtoType: string;
        MembersMetadata: Array<DtoMemberMetadata>;
    }

    export interface AppMetadata {
        Dtos: Array<DtoMetadata>;
        Messages: Array<EnvironmentCulture>;
        Projects: Array<ProjectMetadata>;
        Views: Array<ProjectMetadata>;
    }

    export interface IMetadataProvider {
        getMetadata(): Promise<AppMetadata>;
    }
}