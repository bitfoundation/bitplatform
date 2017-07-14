module Bit.Tests.Implementations {

    @ObjectDependency({
        name: "AppEvent"
    })
    export class TestDefaultAngularAppInitialization extends Bit.Implementations.DefaultAngularAppInitialization {

        public constructor( @Inject("ClientAppProfileManager") public clientAppProfileManager: ClientAppProfileManager) {
            super();
        }

        protected getModuleDependencies(): Array<string> {
            const modules = ["pascalprecht.translate", "ngComponentRouter", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
            if (this.clientAppProfileManager.getClientAppProfile().screenSize == "DesktopAndTablet")
                modules.push("kendo.directives");
            return modules;
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {
            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);
            await super.configureAppModule(app);
        }

        protected async onAppRun(app: ng.IModule): Promise<void> {
            if (this.clientAppProfileManager.getClientAppProfile().culture == "FaIr")
                angular.element(document.body).addClass("k-rtl");
            await super.onAppRun(app);
        }

        protected async buildAppModule(): Promise<ng.IModule> {

            let AppModule: any = function () {

            };

            this.ngAdapter = new ng.upgrade.UpgradeAdapter(ng.core.forwardRef(() => [AppModule]));

            let app = await super.buildAppModule();

            let ng2Component: any = function () {

                this.people = [
                    { id: 1, name: 'Superman' },
                    { id: 2, name: 'Batman' },
                    { id: 5, name: 'BatGirl' },
                    { id: 3, name: 'Robin' },
                    { id: 4, name: 'Flash' }
                ];

                this.ngModelVal = 1;

            };

            ng2Component.annotations = [
                new ng.core.Component({
                    selector: 'ng2-component',
                    template: `
{{ngModelVal}}
<input ngDefaultControl [(ngModel)]="ngModelVal" />
<table>
    <thead>
        <th>Name</th>
    </thead>
    <tbody>
        <tr *ngFor="let person of people">
            <td>{{person.name}}</td>
        </tr>
    </tbody>
</table>`
                })
            ];

            AppModule.annotations = [
                new ng.core.NgModule({
                    imports: [ng.common.CommonModule, ng.platformBrowser.BrowserModule, ng.forms.FormsModule],
                    declarations: [ng2Component]
                })
            ];

            app.directive("ng2Component", this.ngAdapter.downgradeNg2Component(ng2Component));

            return app;
        }
    }
}