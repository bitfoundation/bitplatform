module Foundation.ViewModel.Contracts {
    export interface IDirective {
        getDirectiveFactory(): angular.IDirectiveFactory;
    }
}