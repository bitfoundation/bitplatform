module Bit.Implementations.Identity {

    export class IdentityServerDefaultAngularAppInitialization extends Implementations.DefaultAngularAppInitialization {
        protected getModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }
    }
}