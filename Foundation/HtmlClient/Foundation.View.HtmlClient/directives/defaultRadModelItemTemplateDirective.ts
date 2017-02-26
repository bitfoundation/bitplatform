module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radModelItemTemplate" })
    export class DefaultRadGridRowDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                require: "ngModel",
                restrict: "A",
                scope: false,
                compile: function () {
                    return {
                        pre: function ($scope: ng.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                            const dependencyManager = Core.DependencyManager.getCurrent();
                            const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                            if (attributes.ngModel == null)
                                return;

                            let model = $parse(attributes.ngModel)($scope);

                            if (model == null)
                                return;

                            if (model.innerInstance != null) {
                                model = model.innerInstance();
                                $parse(attributes.ngModel).assign($scope, model);
                            }

                        }
                    };
                }
            });
        }
    }
}