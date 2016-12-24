module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "detailButton" })
    export class DefaultRadGridDetailButton implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                restrict: 'A',
                link: ($scope: ng.IScope, $element: JQuery): void => {
                    $element.addClass('k-hierarchy-cell');
                    $element.append('<a class="k-icon k-i-expand" href="\#"></a>');
                }
            });
        }
    }
}