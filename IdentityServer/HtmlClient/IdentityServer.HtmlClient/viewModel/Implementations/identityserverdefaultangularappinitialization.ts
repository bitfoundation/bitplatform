/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />
/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.core.htmlclient/typings.d.ts" />

module IdentityServer.ViewModel.Implementations {
    export class IdentityServerDefaultAngularAppInitialization extends Foundation.ViewModel.Implementations.DefaultAngularAppInitialization {

        public constructor() {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }
    }
}