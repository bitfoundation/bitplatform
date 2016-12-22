module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridRow" })
    export class DefaultRadGridRowDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                require: "ngModel",
                restrict: "A",
                scope: false,
                link($scope: ng.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                    $timeout(() => {

                        const unRegister = $scope.$watch(attributes.ngModel, (model: any) => {

                            if (model == null)
                                return;

                            unRegister();

                            if (model.innerInstance != null) {
                                model = model.innerInstance();
                                $parse(attributes.ngModel).assign($scope, model);
                            }
                        });
                    });
                }
            });
        }
    }
}