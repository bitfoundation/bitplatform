module Bit.Implementations.Identity {

    export class IdentityServerDefaultAngularAppInitialization extends Implementations.DefaultAngularAppInitialization {

        public constructor() {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }
    }
}