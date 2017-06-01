module BitChangeSetManager.View.Directives {

    @DirectiveDependency({ name: "bitMenuContainer" })
    export class DefaultColorViewerDirective implements FoundationVM.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                template: `<md-sidenav ng-transclude></md-sidenav>`,
                transclude: true,
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes) {

                    let $mdSidenav = DependencyManager.getCurrent().resolveObject<ng.material.ISidenavService>("$mdSidenav")(attributes["mdComponentId"]);

                    element.mouseleave(() => {
                        $mdSidenav.close();
                    });

                    let direction = FoundationCore.ClientAppProfileManager.getCurrent().getClientAppProfile().direction;

                    if (direction == "Rtl")
                        element.addClass("md-sidenav-right");
                    else
                        element.addClass("md-sidenav-left");

                    angular.element(document.body).mousemove((e) => {
                        if ((e.clientX == 0 && direction == "Ltr") || (e.clientX == document.body.clientWidth - 1 && direction == "Rtl")) {
                            if (!$mdSidenav.isOpen()) {
                                $mdSidenav.open();
                            }
                        }
                    });
                }
            });
        }
    }
}