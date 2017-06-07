module Bit.Implementations {

    @Injectable()
    export class DefaultLogger extends LoggerBase {

        public constructor( @InjectAll("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super(entityContextProvider);
        }

    }
}