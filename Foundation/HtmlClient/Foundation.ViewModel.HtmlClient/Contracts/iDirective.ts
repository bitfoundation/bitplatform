module Foundation.ViewModel.Contracts {
    export interface IDirective {
        getDirectiveFactory(): ng.IDirectiveFactory;
    }
}