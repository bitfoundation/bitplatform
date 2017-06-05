module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadModelItemTemplate",
        require: {
            ngModel: "ngModel"
        },
        restrict: "A",
        scope: false,
        compile() {
            return {
                pre($scope: ng.IScope, $element: JQuery, $attrs: ng.IAttributes & { ngModel: string }, ctrl: ng.INgModelController) {

                    const dependencyManager = Core.DependencyManager.getCurrent();
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                    let parsedNgModel = $parse($attrs.ngModel);
                    let model = parsedNgModel($scope);

                    if (model == null)
                        return;

                    if (model.innerInstance != null) {
                        model = model.innerInstance();
                        parsedNgModel.assign($scope, model);
                    }

                }
            };
        }
    })
    export class DefaultRadModelItemTemplateDirective {

    }
}