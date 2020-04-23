/// <reference path="node_modules/@bit/bitframework/typings.all.d.ts" />
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
var BitChangeSetManager;
(function (BitChangeSetManager) {
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
        name: "kendo-common-styles",
        path: "node_modules/kendo-ui/styles/kendo.common.min",
        fileDependencyType: "Style",
        onError: function () {
            console.warn("Download professional version of kendo and copy that to node_modules/kendo-ui/");
        }
    });
    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "node_modules/kendo-ui/styles/kendo.material.min",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "node_modules/kendo-ui/styles/kendo.rtl.min",
        predicate: function (appInfo) {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependencyType: "Style",
        onLoad: function () {
            document.body.className += "k-rtl";
        }
    });
    dependencyManager.registerFileDependency({
        name: "persian-date-picker-styles",
        path: "node_modules/persian-datepicker/dist/css/persian-datepicker.min",
        fileDependencyType: "Style",
        predicate: function (appInfo) { return appInfo.culture == "FaIr"; }
    });
    dependencyManager.registerFileDependency({
        name: "persian-date-picker-blue-styles",
        path: "node_modules/persian-datepicker/dist/css/theme/persian-datepicker-blue.min",
        fileDependencyType: "Style",
        predicate: function (appInfo) { return appInfo.culture == "FaIr"; }
    });
    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "node_modules/@bit/bitframework/contents/styles/controls",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "node_modules/@bit/bitframework/contents/styles/theme.light.blue",
        fileDependencyType: "Style"
    });
    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "node_modules/@bit/bitframework/contents/styles/en-US",
        fileDependencyType: "Style",
        predicate: function (appEnvProvider) { return appEnvProvider.culture == "EnUs"; }
    });
    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "node_modules/@bit/bitframework/contents/styles/fa-IR",
        fileDependencyType: "Style",
        predicate: function (appEnvProvider) { return appEnvProvider.culture == "FaIr"; }
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
        name: "event-source-polyfill",
        path: "node_modules/event-source-polyfill/src/eventsource",
        predicate: function (appInfo) {
            return typeof (window["EventSource"]) == "undefined";
        }
    });
    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "node_modules/regenerator-runtime/runtime"
    });
    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "node_modules/jquery/dist/jquery",
        onLoad: function () {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });
    dependencyManager.registerFileDependency({
        name: "angular",
        path: "node_modules/angular/angular"
    });
    dependencyManager.registerFileDependency({
        name: "kendo-ui-web",
        path: "node_modules/kendo-ui/js/kendo.web.min"
    });
    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "node_modules/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: function (appInfo) {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });
    dependencyManager.registerFileDependency({
        name: "kendo-messages-fa-IR",
        path: "node_modules/kendo-ui/js/messages/kendo.messages.fa-IR.min",
        continueOnError: true,
        predicate: function (appInfo) {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });
    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "node_modules/@bit/jaydata/jaydata"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-inMemory-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/InMemoryProvider"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/IndexedDbProvider"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "node_modules/@bit/jaydata/jaydatamodules/kendo"
    });
    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "node_modules/@bit/jaydata/jaydatamodules/inMemory"
    });
    dependencyManager.registerFileDependency({
        name: "ui-router",
        path: "node_modules/@uirouter/angularjs/release/angular-ui-router"
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
        name: "persian-date",
        path: "node_modules/persian-datepicker/assets/persian-date.min",
        predicate: function (appInfo) { return appInfo.culture == "FaIr"; }
    });
    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "node_modules/persian-datepicker/dist/js/persian-datepicker",
        predicate: function (appInfo) { return appInfo.culture == "FaIr"; }
    });
    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "node_modules/signalr/jquery.signalR"
    });
    dependencyManager.registerFileDependency({
        name: "pubsub-js",
        path: "node_modules/pubsub-js/src/pubsub"
    });
    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "node_modules/decimal.js/decimal"
    });
    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "node_modules/@bit/bitframework/Bit.Model.Context"
    });
    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-context",
        path: "BitChangeSetManager.Model.Context"
    });
})(BitChangeSetManager || (BitChangeSetManager = {}));
/// <reference path="imports.ts" />
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var clientAppProfile = Bit.ClientAppProfileManager.getCurrent().getClientAppProfile();
    var dependencyManager = DependencyManager.getCurrent();
    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.DefaultKendoExtender });
    dependencyManager.registerObjectDependency({ name: "Logger", type: Bit.Implementations.DefaultLogger });
    dependencyManager.registerObjectDependency({ name: "AppStartup", type: Bit.Implementations.DefaultAppStartup });
    dependencyManager.registerObjectDependency({ name: "EntityContextProvider", type: Bit.Implementations.DefaultEntityContextProvider });
    dependencyManager.registerObjectDependency({ name: "AppEvent", type: Bit.Implementations.DefaultEntityContextProviderAppEvent });
    dependencyManager.registerObjectDependency({ name: "MessageReceiver", type: Bit.Implementations.SignalRMessageReceiver });
    dependencyManager.registerObjectDependency({ name: "MetadataProvider", type: Bit.Implementations.DefaultMetadataProvider });
    dependencyManager.registerObjectDependency({ name: "AngularConfiguration", type: Bit.Implementations.DefaultAngularTranslateConfiguration });
    dependencyManager.registerObjectDependency({ name: "DateTimeService", type: Bit.Implementations.DefaultDateTimeService });
    dependencyManager.registerObjectDependency({ name: "SecurityService", type: Bit.Implementations.DefaultSecurityService });
    dependencyManager.registerObjectDependency({ name: "GuidUtils", type: Bit.Implementations.DefaultGuidUtils });
    dependencyManager.registerInstanceDependency({ name: "ClientAppProfileManager" }, Bit.ClientAppProfileManager.getCurrent());
})(BitChangeSetManager || (BitChangeSetManager = {}));
/// <reference path="imports.ts" />
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
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var BitChangeSetManagerAngularAppInitialization = /** @class */ (function (_super) {
        __extends(BitChangeSetManagerAngularAppInitialization, _super);
        function BitChangeSetManagerAngularAppInitialization(clientAppProfileManager) {
            var _this = _super.call(this) || this;
            _this.clientAppProfileManager = clientAppProfileManager;
            return _this;
        }
        BitChangeSetManagerAngularAppInitialization.prototype.getModuleDependencies = function () {
            var modules = ["pascalprecht.translate", "ui.router", "ngMessages", "ngMaterial", "ngAria", "ngAnimate", "kendo.directives"];
            return modules;
        };
        BitChangeSetManagerAngularAppInitialization.prototype.configureAppModule = function (app) {
            return __awaiter(this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            app.config(["$locationProvider", function ($locationProvider) {
                                    $locationProvider.html5Mode(true);
                                }]);
                            app.config(["$stateProvider", "$urlRouterProvider", "$urlServiceProvider", function ($stateProvider, $urlRouterProvider, $urlServiceProvider) {
                                    $urlServiceProvider.rules.otherwise({ state: "changeSetsViewModel" });
                                    $stateProvider.state("changeSetsViewModel", {
                                        url: "/change-sets-page",
                                        component: "changeSetsViewModel"
                                    });
                                }]);
                            return [4 /*yield*/, _super.prototype.configureAppModule.call(this, app)];
                        case 1:
                            _a.sent();
                            return [2 /*return*/];
                    }
                });
            });
        };
        BitChangeSetManagerAngularAppInitialization.prototype.onAppRun = function (app) {
            return __awaiter(this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            Bit.Directives.DefaultRadGridDirective.defaultRadGridDirectiveCustomizers.push(function ($scope, attribues, element, gridOptions) {
                                gridOptions.groupable = true;
                            });
                            return [4 /*yield*/, _super.prototype.onAppRun.call(this, app)];
                        case 1:
                            _a.sent();
                            return [2 /*return*/];
                    }
                });
            });
        };
        BitChangeSetManagerAngularAppInitialization.prototype.registerFilters = function (app) {
            return __awaiter(this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0: return [4 /*yield*/, _super.prototype.registerFilters.call(this, app)];
                        case 1:
                            _a.sent();
                            app.filter("html", ["$sce", function ($sce) {
                                    return function (html) {
                                        return $sce.trustAsHtml(html);
                                    };
                                }]);
                            return [2 /*return*/];
                    }
                });
            });
        };
        BitChangeSetManagerAngularAppInitialization = __decorate([
            ObjectDependency({
                name: "AppEvent"
            }),
            __param(0, Inject("ClientAppProfileManager"))
        ], BitChangeSetManagerAngularAppInitialization);
        return BitChangeSetManagerAngularAppInitialization;
    }(Bit.Implementations.DefaultAngularAppInitialization));
    BitChangeSetManager.BitChangeSetManagerAngularAppInitialization = BitChangeSetManagerAngularAppInitialization;
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var BitChangeSetManagerPathProvider = /** @class */ (function (_super) {
        __extends(BitChangeSetManagerPathProvider, _super);
        function BitChangeSetManagerPathProvider() {
            return _super !== null && _super.apply(this, arguments) || this;
        }
        BitChangeSetManagerPathProvider.prototype.getProjectsPath = function () {
            return [];
        };
        BitChangeSetManagerPathProvider = __decorate([
            ObjectDependency({
                name: "PathProvider"
            })
        ], BitChangeSetManagerPathProvider);
        return BitChangeSetManagerPathProvider;
    }(Bit.Implementations.DefaultPathProvider));
    BitChangeSetManager.BitChangeSetManagerPathProvider = BitChangeSetManagerPathProvider;
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var View;
    (function (View) {
        var Directives;
        (function (Directives) {
            var BackToTopDirective = /** @class */ (function () {
                function BackToTopDirective($element, $window) {
                    this.$element = $element;
                    this.$window = $window;
                }
                BackToTopDirective.prototype.hideShow$elementBasedOnCurrentPosition = function () {
                    if (this.$window.scrollY > 50) {
                        this.$element.fadeIn();
                    }
                    else {
                        this.$element.fadeOut();
                    }
                };
                BackToTopDirective.prototype.$postLink = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            this.$element.addClass("back-to-top");
                            this.$element.click(function () {
                                $("html, body").animate({ scrollTop: 0 }, 100);
                            });
                            this.hideShow$elementBasedOnCurrentPosition();
                            this.$window.addEventListener("scroll", this.hideShow$elementBasedOnCurrentPosition.bind(this));
                            return [2 /*return*/];
                        });
                    });
                };
                BackToTopDirective.prototype.$onDestroy = function () {
                    this.$window.removeEventListener("scroll", this.hideShow$elementBasedOnCurrentPosition);
                };
                __decorate([
                    Command()
                ], BackToTopDirective.prototype, "$postLink", null);
                BackToTopDirective = __decorate([
                    DirectiveDependency({
                        name: "BackToTop",
                        scope: false,
                        terminal: true,
                        restrict: "A",
                    }),
                    __param(0, Inject("$element")), __param(1, Inject("$window"))
                ], BackToTopDirective);
                return BackToTopDirective;
            }());
            Directives.BackToTopDirective = BackToTopDirective;
        })(Directives = View.Directives || (View.Directives = {}));
    })(View = BitChangeSetManager.View || (BitChangeSetManager.View = {}));
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var View;
    (function (View) {
        var Directives;
        (function (Directives) {
            var BitMenuContainerDirective = /** @class */ (function () {
                function BitMenuContainerDirective($element, $attrs, $mdSidenav, clientAppProfileManager, $window) {
                    this.$element = $element;
                    this.$attrs = $attrs;
                    this.$mdSidenav = $mdSidenav;
                    this.clientAppProfileManager = clientAppProfileManager;
                    this.$window = $window;
                }
                BitMenuContainerDirective.prototype.onMouseMove = function (e) {
                    if ((this.direction == "Ltr" && e.clientX <= 5) || (this.direction == "Rtl" && e.clientX >= document.body.clientWidth - 5)) {
                        if (!this.$mdSidenavObject.isOpen()) {
                            this.$mdSidenavObject.open();
                        }
                    }
                };
                BitMenuContainerDirective.prototype.$postLink = function () {
                    var _this = this;
                    this.$mdSidenavObject = this.$mdSidenav(this.$attrs.mdComponentId);
                    this.$element.mouseleave(function () {
                        _this.$mdSidenavObject.close();
                    });
                    this.direction = this.clientAppProfileManager.getClientAppProfile().direction;
                    if (!this.$element.hasClass("md-sidenav-right") && !this.$element.hasClass("md-sidenav-left")) {
                        if (this.direction == "Rtl")
                            this.$element.addClass("md-sidenav-right");
                        else
                            this.$element.addClass("md-sidenav-left");
                    }
                    this.$window.addEventListener("mousemove", this.onMouseMove.bind(this));
                };
                BitMenuContainerDirective.prototype.$onDestroy = function () {
                    this.$window.addEventListener("mousemove", this.onMouseMove);
                };
                __decorate([
                    Command()
                ], BitMenuContainerDirective.prototype, "$postLink", null);
                BitMenuContainerDirective = __decorate([
                    DirectiveDependency({
                        name: "BitMenuContainer",
                        scope: false,
                        replace: true,
                        terminal: true,
                        template: "<md-sidenav ng-transclude></md-sidenav>",
                        transclude: true
                    }),
                    __param(0, Inject("$element")),
                    __param(1, Inject("$attrs")),
                    __param(2, Inject("$mdSidenav")),
                    __param(3, Inject("ClientAppProfileManager")),
                    __param(4, Inject("$window"))
                ], BitMenuContainerDirective);
                return BitMenuContainerDirective;
            }());
            Directives.BitMenuContainerDirective = BitMenuContainerDirective;
        })(Directives = View.Directives || (View.Directives = {}));
    })(View = BitChangeSetManager.View || (BitChangeSetManager.View = {}));
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var ViewModel;
    (function (ViewModel) {
        var ViewModels;
        (function (ViewModels) {
            var App = /** @class */ (function () {
                function App(messageReceiver, $mdSidenav, securityService, entityContextProvider) {
                    this.messageReceiver = messageReceiver;
                    this.$mdSidenav = $mdSidenav;
                    this.securityService = securityService;
                    this.entityContextProvider = entityContextProvider;
                }
                App.prototype.$onInit = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        var context, _a;
                        return __generator(this, function (_b) {
                            switch (_b.label) {
                                case 0: return [4 /*yield*/, this.entityContextProvider.getContext("BitChangeSetManager")];
                                case 1:
                                    context = _b.sent();
                                    _a = this;
                                    return [4 /*yield*/, context.users.getCurrentUser().getValue()];
                                case 2:
                                    _a.user = _b.sent();
                                    return [4 /*yield*/, this.messageReceiver.start()];
                                case 3:
                                    _b.sent();
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                App.prototype.openMenu = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            switch (_a.label) {
                                case 0: return [4 /*yield*/, this.$mdSidenav("menu").open()];
                                case 1:
                                    _a.sent();
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                App.prototype.closeMenu = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            switch (_a.label) {
                                case 0: return [4 /*yield*/, this.$mdSidenav("menu").close()];
                                case 1:
                                    _a.sent();
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                App.prototype.logout = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            this.securityService.logout();
                            return [2 /*return*/];
                        });
                    });
                };
                __decorate([
                    Command()
                ], App.prototype, "$onInit", null);
                __decorate([
                    Command()
                ], App.prototype, "openMenu", null);
                __decorate([
                    Command()
                ], App.prototype, "closeMenu", null);
                __decorate([
                    Command()
                ], App.prototype, "logout", null);
                App = __decorate([
                    ComponentDependency({
                        name: "app",
                        templateUrl: "view/views/app.html"
                    }),
                    __param(0, Inject("MessageReceiver")),
                    __param(1, Inject("$mdSidenav")),
                    __param(2, Inject("SecurityService")),
                    __param(3, Inject("EntityContextProvider"))
                ], App);
                return App;
            }());
            ViewModels.App = App;
        })(ViewModels = ViewModel.ViewModels || (ViewModel.ViewModels = {}));
    })(ViewModel = BitChangeSetManager.ViewModel || (BitChangeSetManager.ViewModel = {}));
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var ViewModel;
    (function (ViewModel) {
        var ViewModels;
        (function (ViewModels) {
            var ChangeSetRules = /** @class */ (function (_super) {
                __extends(ChangeSetRules, _super);
                function ChangeSetRules() {
                    return _super !== null && _super.apply(this, arguments) || this;
                }
                ChangeSetRules.prototype.validateMember = function (memberName, newValue, oldValue) {
                    if (memberName == "Title")
                        this.setMemberValidity("Title", "max-length", newValue == null || newValue.length < 50);
                    else if (memberName == "Description")
                        this.setMemberValidity("Description", "max-length", newValue == null || newValue.length < 200);
                    _super.prototype.validateMember.call(this, memberName, newValue, oldValue);
                };
                ChangeSetRules = __decorate([
                    DtoRulesDependency({ name: "ChangeSetRules" })
                ], ChangeSetRules);
                return ChangeSetRules;
            }(DtoRules));
            ViewModels.ChangeSetRules = ChangeSetRules;
            var ChangeSetViewModel = /** @class */ (function (_super) {
                __extends(ChangeSetViewModel, _super);
                function ChangeSetViewModel($element, entityContextProvider, rules, $mdDialog, $translate) {
                    var _this = _super.call(this) || this;
                    _this.$element = $element;
                    _this.entityContextProvider = entityContextProvider;
                    _this.rules = rules;
                    _this.$mdDialog = $mdDialog;
                    _this.$translate = $translate;
                    _this.changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;
                    return _this;
                }
                ChangeSetViewModel.prototype.$onInit = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        var _a, _b;
                        return __generator(this, function (_c) {
                            switch (_c.label) {
                                case 0:
                                    _a = this;
                                    return [4 /*yield*/, this.entityContextProvider.getContext("BitChangeSetManager")];
                                case 1:
                                    _a.context = _c.sent();
                                    _b = this;
                                    return [4 /*yield*/, this.context.changeSetDescriptionTemplate.getAllTemplates().toArray()];
                                case 2:
                                    _b.templates = _c.sent();
                                    this.provincesDataSource = this.context.provinces.asKendoDataSource();
                                    this.citiesDataSource = this.context.cities.asKendoDataSource({ serverPaging: true, pageSize: 28, serverSorting: true, sort: { field: "Name", dir: "asc" } });
                                    this.citiesDataSource.asChildOf(this.provincesDataSource, ["ProvinceId"], ["Id"]);
                                    this.answersDataSource = this.context.constants.asKendoDataSource();
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                ChangeSetViewModel.prototype.applyTemplate = function (template) {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            this.model.Description = template.Content;
                            return [2 /*return*/];
                        });
                    });
                };
                ChangeSetViewModel.prototype.onSave = function () {
                    if (this.form.isValid() == false || this.model.isValid() == false) {
                        this.$mdDialog.show(this.$mdDialog.alert()
                            .ok(this.$translate.instant("Ok"))
                            .title(this.$translate.instant("ChangeSetDataIsInvalid"))
                            .parent(this.$element));
                        throw new Error("Change set data is invalid");
                    }
                };
                ChangeSetViewModel.prototype.loadCityById = function (args) {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            switch (_a.label) {
                                case 0: return [4 /*yield*/, this.context.cities.first(function (c, id) { return c.Id == id; }, args)];
                                case 1: return [2 /*return*/, _a.sent()];
                            }
                        });
                    });
                };
                __decorate([
                    Command()
                ], ChangeSetViewModel.prototype, "$onInit", null);
                __decorate([
                    Command()
                ], ChangeSetViewModel.prototype, "applyTemplate", null);
                __decorate([
                    Command()
                ], ChangeSetViewModel.prototype, "loadCityById", null);
                ChangeSetViewModel = __decorate([
                    DtoViewModelDependency({
                        name: "changeSetViewModel",
                        templateUrl: "view/views/changeSetView.html",
                        bindings: {
                            changeSetSeveritiesDataSource: '<',
                            changeSetDeliveryRequirementsDataSource: '<'
                        }
                    }),
                    __param(0, Inject("$element")),
                    __param(1, Inject("EntityContextProvider")),
                    __param(2, Inject("ChangeSetRules")),
                    __param(3, Inject("$mdDialog")),
                    __param(4, Inject("$translate"))
                ], ChangeSetViewModel);
                return ChangeSetViewModel;
            }(DtoViewModel));
            ViewModels.ChangeSetViewModel = ChangeSetViewModel;
        })(ViewModels = ViewModel.ViewModels || (ViewModel.ViewModels = {}));
    })(ViewModel = BitChangeSetManager.ViewModel || (BitChangeSetManager.ViewModel = {}));
})(BitChangeSetManager || (BitChangeSetManager = {}));
var BitChangeSetManager;
(function (BitChangeSetManager) {
    var ViewModel;
    (function (ViewModel) {
        var ViewModels;
        (function (ViewModels) {
            var ChangeSetsViewModel = /** @class */ (function () {
                function ChangeSetsViewModel(entityContextProvider, messageReceiver, $mdToast, $translate) {
                    this.entityContextProvider = entityContextProvider;
                    this.messageReceiver = messageReceiver;
                    this.$mdToast = $mdToast;
                    this.$translate = $translate;
                    this.changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;
                    this.deliveryMetadata = BitChangeSetManagerModel.DeliveryDto;
                }
                ChangeSetsViewModel.prototype.$onInit = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        var context, _a, changeSetDeliveryRequirements, changeSetSeverities;
                        return __generator(this, function (_b) {
                            switch (_b.label) {
                                case 0: return [4 /*yield*/, this.entityContextProvider.getContext("BitChangeSetManager")];
                                case 1:
                                    context = _b.sent();
                                    return [4 /*yield*/, context.batchExecuteQuery([context.changeSetDeliveryRequirements, context.changeSetSeverities])];
                                case 2:
                                    _a = _b.sent(), changeSetDeliveryRequirements = _a[0], changeSetSeverities = _a[1];
                                    this.changeSetSeveritiesDataSource = changeSetSeverities.toQueryable(BitChangeSetManagerModel.ChangeSetSeverityDto).asKendoDataSource();
                                    this.changeSetDeliveryRequirementsDataSource = changeSetDeliveryRequirements.toQueryable(BitChangeSetManagerModel.ChangeSetDeliveryRequirementDto).asKendoDataSource();
                                    this.changeSetsDataSource = context.changeSets.asKendoDataSource({ serverPaging: true, pageSize: 5, serverSorting: true, sort: { field: "Title", dir: "asc" } });
                                    this.deliveriesDataSource = context
                                        .deliveries
                                        .map(function (d) { return { Id: d.Id, CustomerName: d.CustomerName, ChangeSetId: d.ChangeSetId, DeliveredOn: d.DeliveredOn }; })
                                        .asKendoDataSource({ serverPaging: true, pageSize: 5, serverSorting: true, sort: { field: "CustomerName", dir: "asc" } });
                                    this.deliveriesDataSource.asChildOf(this.changeSetsDataSource, ["ChangeSetId"], ["Id"]);
                                    this.customersDataSource = context
                                        .customers
                                        .map(function (c) { return { Id: c.Id, Name: c.Name }; })
                                        .asKendoDataSource();
                                    PubSub.subscribe("ChangeSetHasBeenInsertedByUser", this.onChangeSetHasBeenInsertedByUser.bind(this));
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                ChangeSetsViewModel.prototype.onChangeSetHasBeenInsertedByUser = function (key, args) {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            switch (_a.label) {
                                case 0: return [4 /*yield*/, this.$mdToast.show(this.$mdToast.simple()
                                        .textContent(this.$translate.instant("ChangeSetHasBeenInsertedByUser", args))
                                        .hideDelay(3000))];
                                case 1:
                                    _a.sent();
                                    return [2 /*return*/];
                            }
                        });
                    });
                };
                ChangeSetsViewModel.prototype.$onDestroy = function () {
                    return __awaiter(this, void 0, void 0, function () {
                        return __generator(this, function (_a) {
                            PubSub.unsubscribe(this.onChangeSetHasBeenInsertedByUser);
                            return [2 /*return*/];
                        });
                    });
                };
                __decorate([
                    Command()
                ], ChangeSetsViewModel.prototype, "$onInit", null);
                __decorate([
                    Command()
                ], ChangeSetsViewModel.prototype, "onChangeSetHasBeenInsertedByUser", null);
                ChangeSetsViewModel = __decorate([
                    ComponentDependency({
                        name: "changeSetsViewModel",
                        templateUrl: "view/views/changeSetsView.html"
                    }),
                    __param(0, Inject("EntityContextProvider")),
                    __param(1, Inject("MessageReceiver")),
                    __param(2, Inject("$mdToast")),
                    __param(3, Inject("$translate"))
                ], ChangeSetsViewModel);
                return ChangeSetsViewModel;
            }());
            ViewModels.ChangeSetsViewModel = ChangeSetsViewModel;
        })(ViewModels = ViewModel.ViewModels || (ViewModel.ViewModels = {}));
    })(ViewModel = BitChangeSetManager.ViewModel || (BitChangeSetManager.ViewModel = {}));
})(BitChangeSetManager || (BitChangeSetManager = {}));
//# sourceMappingURL=bitChangeSetManager.js.map