module Foundation.ViewModel.Contracts {
    export interface IAngularConfiguration {
        configure(app: angular.IModule): Promise<void>;
    }
}