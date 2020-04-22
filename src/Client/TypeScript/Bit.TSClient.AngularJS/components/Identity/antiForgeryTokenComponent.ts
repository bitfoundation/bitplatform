module Bit.Components.Identity {

    @ComponentDependency({ name: "antiForgeryToken", template: `<input type='hidden' name='{{vm.token.name}}' value='{{vm.token.value}}'>`, bindings: { token: "<" } })
    export class AntiForgeryTokenComponent {

    }
}