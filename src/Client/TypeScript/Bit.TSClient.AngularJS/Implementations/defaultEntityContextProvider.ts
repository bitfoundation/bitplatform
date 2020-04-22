module Bit.Implementations {

    @Injectable()
    export class DefaultEntityContextProvider extends EntityContextProviderBase {

        public constructor(@Inject("GuidUtils") public guidUtils: DefaultGuidUtils, @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider, @Inject("SecurityService") public securityService: Contracts.ISecurityService) {
            super(guidUtils, metadataProvider, securityService);
        }
    }
}
