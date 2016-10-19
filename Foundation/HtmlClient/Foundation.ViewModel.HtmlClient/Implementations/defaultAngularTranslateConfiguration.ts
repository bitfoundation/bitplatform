module Foundation.ViewModel.Implementations {
    export class DefaultAngularTranslateConfiguration implements Contracts.IAngularConfiguration {
        @Core.Log()
        public async configure(app: angular.IModule): Promise<void> {

            let dependencyManager = Core.DependencyManager.getCurrent();

            let metadata = await dependencyManager.resolveObject<Contracts.IMetadataProvider>("MetadataProvider").getMetadata();

            let translates = {};

            metadata.Dtos.forEach(dto => dto.MembersMetadata
                .forEach(member => {
                    member.Messages.forEach(m => {
                        translates[m.Name] = translates[m.Name] || {};
                        m.Values.forEach(v => {
                            translates[m.Name][v.Name] = v.Title;
                        });
                    });
                }));

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

            app.config(['$translateProvider', ($translateProvider: angular.translate.ITranslateProvider) => {

                for (let translate in translates) {
                    if (translates.hasOwnProperty(translate)) {
                        $translateProvider.translations(translate, translates[translate]);
                    }
                }

                $translateProvider.preferredLanguage(Core.ClientAppProfileManager.getCurrent().clientAppProfile.culture);

                $translateProvider.useSanitizeValueStrategy('escape');

            }]);
        }
    }
}