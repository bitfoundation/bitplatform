module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radGridRow' })
    export class DefaultRadGridRowDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                require: 'ngModel',
                restrict: 'A',
                scope: false,
                link($scope: angular.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    let dependencyManager = Core.DependencyManager.getCurrent();

                    let $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");
                    let $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");

                    $timeout(() => {

                        let unRegister = $scope.$watch(attributes.ngModel, (model: any) => {

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