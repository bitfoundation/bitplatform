module Foundation.ViewModel.Contracts {
    export interface IEntityContextProvider {
        getReadContext<TContext extends $data.EntityContext>(contextName: string, config?: { isOffline?: boolean }): Promise<TContext>;
        getContext<TContext extends $data.EntityContext>(contextName: string, config?: { isOffline?: boolean }): Promise<TContext>
    }
}