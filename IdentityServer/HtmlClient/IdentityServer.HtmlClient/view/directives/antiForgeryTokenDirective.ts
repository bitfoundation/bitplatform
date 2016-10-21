/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module IdentityServer.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: "antiForgeryToken" })
    export class AntiForgeryTokenDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return (): angular.IDirective => {
                return {
                    restrict: 'E',
                    replace: true,
                    scope: {
                        token: "="
                    },
                    template: "<input type='hidden' name='{{token.name}}' value='{{token.value}}'>"
                };
            };
        }
    }
}