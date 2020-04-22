module Bit.Implementations {

    @Injectable()
    export class DefaultLogger extends LoggerBase {

        public constructor(@Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super(entityContextProvider);
        }

    }
}