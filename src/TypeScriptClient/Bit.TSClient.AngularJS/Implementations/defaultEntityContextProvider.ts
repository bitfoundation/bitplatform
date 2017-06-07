module Bit.Implementations {

    @Injectable()
    export class DefaultEntityContextProvider extends EntityContextProviderBase {

        public constructor( @Inject("GuidUtils") public guidUtils: DefaultGuidUtils, @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider) {
            super(guidUtils, metadataProvider);
        }
    }
}
