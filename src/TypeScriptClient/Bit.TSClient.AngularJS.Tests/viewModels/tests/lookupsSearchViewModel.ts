module Bit.Tests.ViewModels {

    @SecureFormViewModelDependency({
        name: "LookupsSearchViewModel", template: `

<dto-form ng-model="vm.product">
    <rad-combo-box rad-data-source="vm.countriesDataSource" ng-model="vm.product.BuildLocationId" />
    <br />
    {{vm.product.BuildLocationId}}
    {{vm.countriesDataSource.current.Name}}
</dto-form>

` })
    export class LookupsSearchViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("EntityContextProvider") public entityContextProvider: Contracts.IEntityContextProvider) {
            super();
        }

        public product = new BitTestsModel.ProductDto();

        public countriesDataSource: kendo.data.DataSource;

        @Command()
        public async $onInit(): Promise<void> {
            let onlineContext = await this.entityContextProvider.getContext<TestContext>("Test");
            this.countriesDataSource = onlineContext.countries.getAllContries().asKendoDataSource();
        }
    }

    @ComponentDependency({
        name: "CountryDtoSearchComponent",
        template: `
 <rad-grid rad-data-source="vm.filterDataSource">
    <view-template>
        <columns>
            <column name="Id" title="Id"></column>
            <column name="Name" title="Name"></column>
        </columns>
    </view-template>
</rad-grid>
`,
        bindings: {
            filterDataSource: "<"
        }
    })
    export class CountryDtoSearchComponent {

    }
}