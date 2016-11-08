/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module IdentityServer.View.Directives {

    @Foundation.Core.ComponentDependency({ name: "antiForgeryToken", template: `<input type='hidden' name='{{vm.token.name}}' value='{{vm.token.value}}'>`, bindings: { token: "<" } })
    export class AntiForgeryTokenComponent {

    }
}