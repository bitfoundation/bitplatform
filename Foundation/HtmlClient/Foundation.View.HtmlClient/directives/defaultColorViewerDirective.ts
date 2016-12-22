
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "colorViewer" })
    export class DefaultColorViewerDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                template: (element: JQuery, attrs: angular.IAttributes) => {
                    return `<div style='background-color:${attrs["color"]}; color:transparent;'>color</div>`;
                }
            });
        }
    }
}