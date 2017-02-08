module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "dtoFormService" })
    export class DefaultDtoFormServiceDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                restrict: "A",
                require: "ngModel",
                scope: false,
                link($scope: ng.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    let defaultDtoViewModel: ViewModel.Implementations.DefaultDtoViewModel<Model.Contracts.IDto, ViewModel.Implementations.DtoRules<Model.Contracts.IDto>> = null;
                    let dtoViewModel: ViewModel.Contracts.IDtoViewModel<Model.Contracts.IDto> = null;
                    let dtoRules: ViewModel.Implementations.DtoRules<Model.Contracts.IDto> = null;

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");
                    const dateTimeService = dependencyManager.resolveObject<ViewModel.Contracts.IDateTimeService>("DateTimeService");
                    const clientAppProfile = dependencyManager.resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager").getClientAppProfile();

                    $scope.$on("$destroy", () => {

                        if (defaultDtoViewModel != null) {

                            if (defaultDtoViewModel.model != null) {
                                if (defaultDtoViewModel['propertyChangedFunction'] != null) {
                                    defaultDtoViewModel.model.propertyChanged.detach(dtoViewModel['propertyChangedFunction']);
                                    defaultDtoViewModel.model.propertyChanging.detach(dtoViewModel['propertyChangingFunction']);
                                }
                                defaultDtoViewModel.model = null;
                            }

                            defaultDtoViewModel.form = null;

                        }

                        if (dtoRules != null && dtoRules.model != null) {
                            if (dtoRules['propertyChangedFunction'] != null) {
                                dtoRules.model.propertyChanged.detach(dtoRules['propertyChangedFunction']);
                            }
                            dtoRules.model = null;
                        }

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

                                            if (propDefenition.dataType == "Edm.DateTimeOffset" || propDefenition.dataType == $data["DateTimeOffset"]) {
                                                propModelController.$parsers.push((viewValue) => {
                                                    if (viewValue != null && !(viewValue instanceof Date)) {
                                                        viewValue = dateTimeService.parseDate(viewValue);
                                                    }
                                                    return viewValue;
                                                });
                                            }

                                            propModelController['hadRequired'] = propModelController.$validators.required != null;
                                            propModelController['hadPattern'] = propModelController.$validators.regex != null;
                                        }

                                        if (propDefenition.required == true && propModelController['hadRequired'] == false) {

                                            if (newModel != null) {
                                                propModelController.$validators.required = (modelValue, viewValue) => {

                                                    let modelIsValid = newModel.isValid();

                                                    if (modelIsValid == true)
                                                        return true;

                                                    modelIsValid = newModel.ValidationErrors
                                                        .find(vErr => vErr.Type == "required" && vErr.PropertyDefinition == propDefenition) == null;

                                                    return modelIsValid;
                                                };
                                            }
                                            else {
                                                delete propModelController.$validators.required;
                                            }

                                        }

                                        if (propDefenition.regex != null && propModelController['hadPattern'] == false) {

                                            if (newModel != null) {
                                                propModelController.$validators.pattern = (modelValue, viewValue) => {

                                                    let modelIsValid = newModel.isValid();

                                                    if (modelIsValid == true)
                                                        return true;

                                                    modelIsValid = newModel.ValidationErrors
                                                        .find(vErr => vErr.Type == "regex" && vErr.PropertyDefinition == propDefenition) == null;

                                                    return modelIsValid;
                                                };
                                            }
                                            else {
                                                delete propModelController.$validators.pattern;
                                            }
                                        }

                                        propModelController.validityEvaludated = false;

                                        propModelControllers.push(propModelController);
                                    }
                                }
                            }

                            if (attributes.dtoViewModel != null) {
                                dtoViewModel = $parse(attributes.dtoViewModel)($scope);
                                if (dtoViewModel instanceof ViewModel.Implementations.DefaultDtoViewModel) {
                                    defaultDtoViewModel = dtoViewModel as ViewModel.Implementations.DefaultDtoViewModel<Model.Contracts.IDto, ViewModel.Implementations.DtoRules<Model.Contracts.IDto>>;
                                }
                            }

                            if (defaultDtoViewModel != null)
                                dtoRules = defaultDtoViewModel.rules;
                            else if (attributes.dtoRules != null)
                                dtoRules = $parse(attributes.dtoRules)($scope);

                            if (dtoRules != null && (dtoRules.model == null || dtoRules.model == oldModel))
                                dtoRules.model = newModel;

                            if (defaultDtoViewModel != null) {

                                if (defaultDtoViewModel.model == null || defaultDtoViewModel.model == oldModel)
                                    defaultDtoViewModel.model = newModel;

                                if (defaultDtoViewModel.form == null)
                                    defaultDtoViewModel.form = ctrl.$$parentForm;

                                (async function () {
                                    try {
                                        await defaultDtoViewModel.onActivated();
                                    }
                                    finally {
                                        ViewModel.ScopeManager.update$scope($scope);
                                    }
                                })();
                            }

                            if (dtoViewModel != null) {

                                if (dtoViewModel['propertyChangedFunction'] == null) {
                                    dtoViewModel['propertyChangedFunction'] = function propertyChangedFunction(sender, e) {
                                        if (e.oldValue != e.newValue) {
                                            dtoViewModel.onMemberChanged(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (dtoViewModel['propertyChangingFunction'] == null) {
                                    dtoViewModel['propertyChangingFunction'] = function propertyChangingFunction(sender, e) {
                                        if (e.oldValue != e.newValue) {
                                            dtoViewModel.onMemberChanging(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (newModel != null) {
                                    newModel.propertyChanged.attach(dtoViewModel['propertyChangedFunction']);
                                    newModel.propertyChanging.attach(dtoViewModel['propertyChangingFunction']);
                                }

                                if (oldModel != null) {
                                    oldModel.propertyChanged.detach(dtoViewModel['propertyChangedFunction']);
                                    oldModel.propertyChanging.detach(dtoViewModel['propertyChangingFunction']);
                                }
                            }

                            if (dtoRules != null) {

                                if (dtoRules['propertyChangedFunction'] == null) {
                                    dtoRules['propertyChangedFunction'] = function propertyChangedFunction(sender, e) {
                                        if (e.oldValue != e.newValue) {
                                            dtoRules.validateMember(e.propertyName, e.newValue, e.oldValue);
                                        }
                                    };
                                }

                                if (newModel != null)
                                    newModel.propertyChanged.attach(dtoRules['propertyChangedFunction']);

                                if (oldModel != null)
                                    oldModel.propertyChanged.detach(dtoRules['propertyChangedFunction']);

                                if (dtoRules['prevValidationsRollbackHandlers'] != null) {
                                    for (let prevValidationsRollbackHandler of dtoRules['prevValidationsRollbackHandlers']) {
                                        prevValidationsRollbackHandler.handler();
                                    }
                                }

                                dtoRules['prevValidationsRollbackHandlers'] = [];

                                dtoRules.setMemberValidaty = (memberName: string, errorKey: string, isValid: boolean): void => {
                                    const propModelCtrl = propModelControllers.find(p => p.$name == memberName);
                                    if (propModelCtrl != null) {
                                        propModelCtrl.$setValidity(errorKey, isValid);
                                        if (isValid == false) {
                                            if (!dtoRules['prevValidationsRollbackHandlers'].some(h => h.memberName == memberName && h.errorKey == errorKey)) {
                                                dtoRules['prevValidationsRollbackHandlers'].push({
                                                    memberName: memberName, errorKey: errorKey, handler: () => {
                                                        propModelCtrl.$setValidity(errorKey, true);
                                                        propModelCtrl.validityEvaludated = false;
                                                    }
                                                });
                                            }
                                        }
                                        else {
                                            dtoRules['prevValidationsRollbackHandlers'] = dtoRules['prevValidationsRollbackHandlers'].filter(h => h.memberName != memberName && h.errorKey != errorKey);
                                        }
                                    }
                                    else {
                                        if (clientAppProfile.isDebugMode == true)
                                            console.warn(`No Prop named ${memberName} is in dto form`);
                                    }
                                };
                            }

                        });
                    });
                }
            });
        }
    }
}