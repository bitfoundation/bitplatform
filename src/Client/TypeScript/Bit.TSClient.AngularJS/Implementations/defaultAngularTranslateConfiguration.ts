module Bit.Implementations {

    let dependencyManager = DependencyManager.getCurrent();

    export class DefaultAngularTranslateConfiguration implements Contracts.IAngularConfiguration {

        public constructor(public metadataProvider = dependencyManager.resolveObject<Contracts.IMetadataProvider>("MetadataProvider"), public clientAppProfileManager = ClientAppProfileManager.getCurrent()) {

        }

        @Log()
        public async configure(app: ng.IModule): Promise<void> {

            const currentCulture = this.clientAppProfileManager.getClientAppProfile().culture;

            const metadata = await this.metadataProvider.getMetadata();

            const translates = {};

            metadata.Dtos.forEach(dto => {

                const parts = dto.DtoType.split(".");
                let jayDataDtoType: any = window;
                for (let part of parts) {
                    jayDataDtoType = jayDataDtoType[part];
                    if (jayDataDtoType == null) {
                        return;
                    }
                }

                dto.MembersMetadata
                    .forEach(member => {

                        let dtoMemberMetadata = jayDataDtoType[member.DtoMemberName] = jayDataDtoType[member.DtoMemberName] || {};
                        dtoMemberMetadata = dtoMemberMetadata || {};

                        member.Messages.forEach(m => {
                            if (m.Name == currentCulture) {
                                m.Values.forEach(v => {
                                    dtoMemberMetadata[v.Name] = v.Title;
                                });
                            }
                        });
                    });
            });

            metadata.Projects.forEach(proj => proj.Messages
                .forEach(m => {
                    translates[m.Name] = translates[m.Name] || {};
                    m.Values.forEach(v => {
                        translates[m.Name][v.Name] = v.Title;
                    });
                }));

            metadata.Views.forEach(view => view.Messages
                .forEach(m => {
                    translates[m.Name] = translates[m.Name] || {};
                    m.Values.forEach(v => {
                        translates[m.Name][v.Name] = v.Title;
                    });
                }));

            metadata.Messages
                .forEach(m => {
                    translates[m.Name] = translates[m.Name] || {};
                    m.Values.forEach(v => {
                        translates[m.Name][v.Name] = v.Title;
                    });
                });

            app.config(["$translateProvider", ($translateProvider: ng.translate.ITranslateProvider) => {

                for (let translate in translates) {
                    if (translates.hasOwnProperty(translate)) {
                        $translateProvider.translations(translate, translates[translate]);
                    }
                }

                $translateProvider.preferredLanguage(currentCulture);

                $translateProvider.useSanitizeValueStrategy("escape");

            }]);
        }
    }
}