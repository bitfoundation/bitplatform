/// <reference path="../node_modules/@bit/bitframework/typings.all.d.ts" />
var DtoViewModelDependency = Bit.DtoViewModelDependency;
var DtoRulesDependency = Bit.DtoRulesDependency;
var ComponentDependency = Bit.ComponentDependency;
var DirectiveDependency = Bit.DirectiveDependency;
var ObjectDependency = Bit.ObjectDependency;
var Inject = Bit.Inject;
var InjectAll = Bit.InjectAll;
var ClientAppProfileManager = Bit.ClientAppProfileManager;
var DependencyManager = Bit.DependencyManager;
var Log = Bit.Log;
var DtoViewModel = Bit.ViewModels.DtoViewModel;
var DtoRules = Bit.Implementations.DtoRules;
var Command = Bit.Command;
/// <reference path="imports.ts" />
var BitChangeSetManagerSecurity;
(function (BitChangeSetManagerSecurity) {
    var dependencyManager = DependencyManager.getCurrent();
    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "node_modules/normalize-css/normalize",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "node_modules/angular-material/angular-material",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "node_modules/@bit/bitframework/contents/styles/controls",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "node_modules/@bit/bitframework/contents/styles/en-US",
        fileDependencyType: "Style",
        predicate: function (appEnvProvider) { return appEnvProvider.culture == "EnUs"; }
    });
    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-styles",
        path: "view/styles/bitChangeSetManagerStyles",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "node_modules/core-js/client/core"
    });
    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "node_modules/whatwg-fetch/fetch",
        predicate: function (appInfo) {
            return typeof (fetch) == "undefined";
        }
    });
    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "node_modules/regenerator-runtime/runtime"
    });
    dependencyManager.registerFileDependency({
        name: "angular",
        path: "node_modules/angular/angular"
    });
    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "node_modules/@bit/jaydata/jaydata"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });
    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "node_modules/angular-animate/angular-animate"
    });
    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "node_modules/angular-aria/angular-aria"
    });
    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "node_modules/angular-material/angular-material"
    });
    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "node_modules/angular-messages/angular-messages"
    });
    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "node_modules/angular-translate/dist/angular-translate"
    });
    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "node_modules/@bit/bitframework/Bit.Model.Context"
    });
})(BitChangeSetManagerSecurity || (BitChangeSetManagerSecurity = {}));
/// <reference path="imports.ts" />
var BitChangeSetManagerSecurity;
(function (BitChangeSetManagerSecurity) {
    var dependencyManager = DependencyManager.getCurrent();
    dependencyManager.registerObjectDependency({ name: "LoginModelProvider", type: Bit.Implementations.Identity.DefaultLoginModelProvider });
    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.Identity.IdentityServerDefaultAngularAppInitialization });
    dependencyManager.registerObjectDependency({ name: "Logger", type: Bit.Implementations.DefaultLogger });
    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Bit.Implementations.DefaultAppStartup });
    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Bit.Implementations.DefaultMetadataProvider });
    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Bit.Implementations.DefaultAngularTranslateConfiguration });
    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, Bit.ClientAppProfileManager.getCurrent());
    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Bit.Implementations.DefaultDateTimeService });
    dependencyManager.registerObjectDependency({ name: "SecurityService", type: Bit.Implementations.DefaultSecurityService });
    dependencyManager.registerObjectDependency({ name: "GuidUtils", type: Bit.Implementations.DefaultGuidUtils });
    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Bit.Implementations.DefaultEntityContextProvider });
})(BitChangeSetManagerSecurity || (BitChangeSetManagerSecurity = {}));
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var BitChangeSetManagerSecurity;
(function (BitChangeSetManagerSecurity) {
    var BitChangeSetManagerSecurityPathProvider = /** @class */ (function (_super) {
        __extends(BitChangeSetManagerSecurityPathProvider, _super);
        function BitChangeSetManagerSecurityPathProvider() {
            return _super !== null && _super.apply(this, arguments) || this;
        }
        BitChangeSetManagerSecurityPathProvider.prototype.getProjectsPath = function () {
            return [];
        };
        BitChangeSetManagerSecurityPathProvider = __decorate([
            ObjectDependency({
                name: "PathProvider"
            })
        ], BitChangeSetManagerSecurityPathProvider);
        return BitChangeSetManagerSecurityPathProvider;
    }(Bit.Implementations.DefaultPathProvider));
    BitChangeSetManagerSecurity.BitChangeSetManagerSecurityPathProvider = BitChangeSetManagerSecurityPathProvider;
})(BitChangeSetManagerSecurity || (BitChangeSetManagerSecurity = {}));
//# sourceMappingURL=bitChangeSetManager-security.js.map