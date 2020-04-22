module Bit.Implementations {

    @Injectable()
    export class DefaultEntityContextProviderAppEvent extends EntityContextProviderAppEventBase {

        public constructor(@Inject("EntityContextProvider") public entityContextProvider: Implementations.EntityContextProviderBase) {
            super(entityContextProvider);
        }
    }
}
