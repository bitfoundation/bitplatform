module Foundation.ViewModel.Contracts {
    export interface IEntityContextProvider {
        getContext<TContext extends $data.EntityContext>(contextName: string, config?: { isOffline?: boolean }): Promise<TContext>
    }
}