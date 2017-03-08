module BitChangeSetManager.ViewModels {

    @FormViewModelDependency({
        name: "App",
        template: `<main ng-model-options="{ updateOn : 'default blur' , allowInvalid : true , debounce: { 'default': 250, 'blur': 0 } }">
                        <ng-outlet></ng-outlet>
                   </main>`,
        $routeConfig: [
            { path: "/change-sets-page", name: "ChangeSetsViewModel", useAsDefault: true }
        ]
    })
    export class App extends SecureViewModel {

    }

}