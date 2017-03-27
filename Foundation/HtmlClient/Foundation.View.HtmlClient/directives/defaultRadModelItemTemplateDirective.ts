module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radModelItemTemplate" })
    export class DefaultRadModelItemTemplateDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                require: "ngModel",
                restrict: "A",
                scope: false,
                compile() {
                    return {
                        pre: function ($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, ctrl: ng.INgModelController) {

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