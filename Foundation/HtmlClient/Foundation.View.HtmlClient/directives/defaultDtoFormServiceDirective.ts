module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "dtoFormService" })
    export class DefaultDtoFormServiceDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                restrict: "A",
                require: "ngModel",
                scope: false,
                link($scope: ng.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    let dtoViewModel: ViewModel.ViewModels.DtoViewModel<Model.Contracts.IDto, ViewModel.Implementations.DtoRules<Model.Contracts.IDto>> = null;
                    let dtoRules: ViewModel.Implementations.DtoRules<Model.Contracts.IDto> = null;
                    let perDtoFormStorage: {
                        prevValidationsRollbackHandlers?: Array<{ memberName: string, errorKey: string, handler: () => void }>,
                        dtoViewModelPropertyChangedFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void,
                        dtoViewModelPropertyChangingFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void,
                        dtoRulesPropertyChangedFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void
                    } = {};

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");
                    const dateTimeService = dependencyManager.resolveObject<ViewModel.Contracts.IDateTimeService>("DateTimeService");
                    const clientAppProfile = dependencyManager.resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager").getClientAppProfile();

                    let cleanUp = () => {

                        if (dtoViewModel != null) {

                            if (dtoViewModel.model != null && dtoViewModel.form != null) {
                                if (perDtoFormStorage.dtoViewModelPropertyChangedFunction != null) {
                                    try {
                                        dtoViewModel.model.propertyChanged.detach(perDtoFormStorage.dtoViewModelPropertyChangedFunction);
                                    }
                                    catch (e) { }
                                }
                                if (perDtoFormStorage.dtoViewModelPropertyChangingFunction != null) {
                                    try {
                                        dtoViewModel.model.propertyChanging.detach(perDtoFormStorage.dtoViewModelPropertyChangingFunction);
                                    }
                                    catch (e) { }
                                }

                                dtoViewModel.form = null;
                            }

                        }

                        if (dtoRules != null) {

                            if (dtoRules.model != null && perDtoFormStorage.dtoRulesPropertyChangedFunction != null) {

                                try {
                                    dtoRules.model.propertyChanged.detach(perDtoFormStorage.dtoRulesPropertyChangedFunction);
                                }
                                catch (e) { }

                                if (perDtoFormStorage.prevValidationsRollbackHandlers != null) {

                                    for (let prevValidationsRollbackHandler of perDtoFormStorage.prevValidationsRollbackHandlers) {
                                        prevValidationsRollbackHandler.handler();
                                    }

                                    perDtoFormStorage.prevValidationsRollbackHandlers = null;
                                }

                            }
                        }

                        perDtoFormStorage = {};

                    };

                    $scope.$on("$destroy", () => {
                        cleanUp();
                    });

                    $timeout(() => {

                        $scope.$watch(attributes.ngModel, (newModel: any, oldModel: any) => {

                            if (newModel == null && oldModel == null)
                                return;

                            if (newModel == oldModel)
                                oldModel = null;

                            if (newModel != null && newModel.innerInstance != null) {
                                newModel = newModel.innerInstance();
                                $parse(attributes.ngModel).assign($scope, newModel);
                            }

                            if (newModel != null && !(newModel instanceof $data.Entity))
                                throw new Error("Can't validate non-entity | non-dto models");

                            let modelType = newModel != null ? newModel.getType() : oldModel.getType();

                            let memberDefenitions = modelType.memberDefinitions;

                            let propModelControllers = [];

                            ctrl.$$parentForm.isValid = (): boolean => {
                                let isValid = true;
                                propModelControllers.forEach(p => {
                                    if (dtoRules != null && newModel != null)
                                        dtoRules.validateMember(p.$name, newModel[p.$name], newModel[p.$name]);
                                    p.$validate();
                                    isValid = isValid && p.$valid;
                                });
                                return isValid;
                            };

                            ctrl.$$parentForm.$setPristine();
                            ctrl.$$parentForm.$setUntouched();

                            for (let prp in ctrl.$$parentForm) {

                                if (ctrl.$$parentForm.hasOwnProperty(prp)) {

                                    let propDefenition = memberDefenitions[`$${prp}`];

                                    if (propDefenition != null && propDefenition.kind == "property") {

                                        let propModelController = ctrl.$$parentForm[prp];

                                        Object.defineProperty(propModelController, "visible", {
                                            configurable: true,
                                            set: (isVisible: boolean) => {
                                                const currentForm = element;
                                                let currentItem = angular.element(element).find(`[name='${propDefenition.name}']`);
                                                const data = currentItem.data();
                                                if (data != null && data["handler"] != null && data["handler"].wrapper != null)
                                                    currentItem = data["handler"].wrapper;

                                                if (isVisible == true) {
                                                    currentItem.show();
                                                    currentItem.parents("md-input-container").show();
                                                }
                                                else {
                                                    currentItem.hide();
                                                    currentItem.parents("md-input-container").hide();
                                                }
                                            },
                                            get: () => {
                                                throw new Error("Return prop is visible or not is not implemented yet");
                                            }
                                        });

                                        Object.defineProperty(propModelController, "editable", {
                                            configurable: true,
                                            set: (isEditable: boolean) => {
                                                const currentForm = element;
                                                let currentItem = angular.element(element).find(`[name='${propDefenition.name}']`);
                                                const data = currentItem.data();
                                                if (data != null && data["handler"] != null && data["handler"].wrapper != null) {
                                                    currentItem = data["handler"].wrapper;
                                                    if (data["handler"]["readonly"] != null)
                                                        data["handler"]["readonly"](!isEditable);
                                                }

                                                currentItem.prop("readonly", !isEditable);
                                            },
                                            get: () => {
                                                throw new Error("Return prop is editable or not is not implemented yet");
                                            }
                                        });

                                        if (propModelController['isFirstTimeIndicator'] == null) {

                                            propModelController['isFirstTimeIndicator'] = {};

                                            let original$setValidity = propModelController.$setValidity;

                                            propModelController.$setValidity = function () {
                                                propModelController.validityEvaludated = true;
                                                return original$setValidity.apply(propModelController, arguments);
                                            }

                                            if (propDefenition.nullable == true) {
                                                propModelController.$parsers.push((viewValue) => {
                                                    if (viewValue == "") {
                                                        viewValue = null;
                                                    }
                                                    return viewValue;
                                                });
                                            }

                                            if (propDefenition.originalType == "Edm.DateTimeOffset") {
                                                propModelController.$parsers.push((viewValue) => {
                                                    if (viewValue != null && !(viewValue instanceof Date)) {
                                                        viewValue = dateTimeService.parseDate(viewValue);
                                                    }
                                                    return viewValue;
                                                });
                                            }
                                            if (propDefenition.originalType == "Edm.Decimal" || propDefenition.originalType == "Edm.Double" || propDefenition.originalType == "Edm.Single") {
                                                propModelController.$parsers.push((viewValue) => {
                                                    if (viewValue != null && typeof viewValue == "string") {
                                                        let viewValueAsString = viewValue as string;
                                                        if (viewValueAsString.startsWith('.')) {
                                                            viewValueAsString = '0' + viewValueAsString;
                                                        }
                                                        if (viewValueAsString.endsWith('.')) {
                                                            viewValueAsString = viewValueAsString + '0';
                                                        }
                                                        viewValue = viewValueAsString;
                                                    }
                                                    return viewValue;
                                                });
                                            }
                                        }

                                        propModelController.validityEvaludated = false;

                                        propModelControllers.push(propModelController);
                                    }
                                }
                            }

                            if (attributes.dtoViewModel != null) {
                                dtoViewModel = $parse(attributes.dtoViewModel)($scope);
                            }

                            if (attributes.dtoRules != null)
                                dtoRules = $parse(attributes.dtoRules)($scope);
                            else if (dtoViewModel != null)
                                dtoRules = dtoViewModel.rules;

                            if (oldModel != null)
                                cleanUp();

                            if (dtoRules != null) {

                                if (!(dtoRules instanceof ViewModel.Implementations.DtoRules)) {
                                    throw new Error(`dto rules is not instance of dto rules`);
                                }

                                if (dtoRules.model == null || dtoRules.model == oldModel)
                                    dtoRules.model = newModel;

                                if (perDtoFormStorage.dtoRulesPropertyChangedFunction == null) {
                                    perDtoFormStorage.dtoRulesPropertyChangedFunction = function propertyChangedFunction(sender, e) {
                                        if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                            dtoRules.validateMember(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (newModel != null)
                                    newModel.propertyChanged.attach(perDtoFormStorage.dtoRulesPropertyChangedFunction);

                                perDtoFormStorage.prevValidationsRollbackHandlers = [];

                                dtoRules.setMemberValidaty = (memberName: string, errorKey: string, isValid: boolean): void => {
                                    const propModelCtrl = propModelControllers.find(p => p.$name == memberName);
                                    if (propModelCtrl != null) {
                                        propModelCtrl.$setValidity(errorKey, isValid);
                                        if (isValid == false) {
                                            if (!perDtoFormStorage.prevValidationsRollbackHandlers.some(h => h.memberName == memberName && h.errorKey == errorKey)) {
                                                perDtoFormStorage.prevValidationsRollbackHandlers.push({
                                                    memberName: memberName, errorKey: errorKey, handler: () => {
                                                        propModelCtrl.$setValidity(errorKey, true);
                                                        propModelCtrl.validityEvaludated = false;
                                                    }
                                                });
                                            }
                                        }
                                        else {
                                            perDtoFormStorage.prevValidationsRollbackHandlers = perDtoFormStorage.prevValidationsRollbackHandlers.filter(h => h.memberName != memberName && h.errorKey != errorKey);
                                        }
                                    }
                                    else {
                                        if (clientAppProfile.isDebugMode == true)
                                            console.warn(`No Prop named ${memberName} is in dto form`);
                                    }
                                };

                                (async function () {
                                    try {
                                        await dtoRules.onActivated();
                                    }
                                    finally {
                                        ViewModel.ScopeManager.update$scope($scope);
                                    }
                                })();
                            }

                            if (dtoViewModel != null) {

                                if (!(dtoViewModel instanceof ViewModel.ViewModels.DtoViewModel)) {
                                    throw new Error(`dto view model ${attributes.dtoViewModel} is not instance of dto view model`);
                                }

                                if (dtoViewModel.model == null || dtoViewModel.model == oldModel)
                                    dtoViewModel.model = newModel;

                                dtoViewModel.form = ctrl.$$parentForm;

                                if (perDtoFormStorage.dtoViewModelPropertyChangedFunction == null) {
                                    perDtoFormStorage.dtoViewModelPropertyChangedFunction = function propertyChangedFunction(sender, e) {
                                        if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                            dtoViewModel.onMemberChanged(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (perDtoFormStorage.dtoViewModelPropertyChangingFunction == null) {
                                    perDtoFormStorage.dtoViewModelPropertyChangingFunction = function propertyChangingFunction(sender, e) {
                                        if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                            dtoViewModel.onMemberChanging(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (newModel != null) {
                                    newModel.propertyChanged.attach(perDtoFormStorage.dtoViewModelPropertyChangedFunction);
                                    newModel.propertyChanging.attach(perDtoFormStorage.dtoViewModelPropertyChangingFunction);
                                }

                                (async function () {
                                    try {
                                        await dtoViewModel.onActivated();
                                    }
                                    finally {
                                        ViewModel.ScopeManager.update$scope($scope);
                                    }
                                })();
                            }

                            if (dtoRules != null && dtoViewModel != null && dtoRules.model != dtoViewModel.model)
                                throw new Error("Dto rules and forms are not using the same model instance");
                            if (dtoRules != null && dtoRules.model != newModel)
                                throw new Error("dto rules's model is not using the same instance of ng-model of dto-form");
                            if (dtoViewModel != null && dtoViewModel.model != newModel)
                                throw new Error("dto view models's model is not using the same instance of ng-model of dto-form");
                        });
                    });
                }
            });
        }
    }
}