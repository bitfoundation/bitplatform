module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "dtoFormService" })
    export class DefaultDtoFormServiceDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                restrict: "A",
                require: "ngModel",
                scope: false,
                link($scope: angular.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    let defaultDtoViewModel: ViewModel.Implementations.DefaultDtoViewModel<Model.Contracts.IDto, ViewModel.Implementations.DtoRules<Model.Contracts.IDto>> = null;
                    let dtoViewModel: ViewModel.Contracts.IDtoViewModel<Model.Contracts.IDto> = null;
                    let dtoRules: ViewModel.Implementations.DtoRules<Model.Contracts.IDto> = null;

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");
                    const dateTimeService = dependencyManager.resolveObject<ViewModel.Contracts.IDateTimeService>("DateTimeService");
                    const clientAppProfile = dependencyManager.resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager").getClientAppProfile();

                    $timeout(() => {

                        const unRegister = $scope.$watch(attributes.ngModel, (model: any) => {

                            if (model == null)
                                return;

                            unRegister();

                            if (model.innerInstance != null) {
                                model = model.innerInstance();
                                $parse(attributes.ngModel).assign($scope, model);
                            }

                            if (!(model instanceof $data.Entity))
                                throw new Error("Can't validate non-entity | non-dto models");

                            let modelType = model.getType();

                            let memberDefenitions = modelType.memberDefinitions;

                            ctrl.$$parentForm.isValid = (): boolean => {
                                let isValid = true;
                                propModelControllers.forEach(p => {
                                    if (dtoRules != null)
                                        dtoRules.validateMember(p.$name, model[p.$name], model[p.$name]);
                                    p.$validate();
                                    isValid = isValid && p.$valid;
                                });
                                return isValid;
                            };

                            let propModelControllers = [];

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

                                        let original$setValidity = propModelController.$setValidity;

                                        propModelController.validityEvaludated = false;

                                        propModelController.$setValidity = function () {
                                            propModelController.validityEvaludated = true;
                                            return original$setValidity.apply(propModelController, arguments);
                                        }

                                        propModelControllers.push(propModelController);

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

                                        if (propDefenition.required == true && propModelController.$validators.required == null) {

                                            propModelController.$validators.required = (modelValue, viewValue) => {

                                                let modelIsValid = model.isValid();

                                                if (modelIsValid == true)
                                                    return true;

                                                modelIsValid = model.ValidationErrors
                                                    .find(vErr => vErr.Type == "required" && vErr.PropertyDefinition == propDefenition) == null;

                                                return modelIsValid;
                                            };
                                        }

                                        if (propDefenition.regex != null && propModelController.$validators.pattern == null) {

                                            propModelController.$validators.pattern = (modelValue, viewValue) => {

                                                let modelIsValid = model.isValid();

                                                if (modelIsValid == true)
                                                    return true;

                                                modelIsValid = model.ValidationErrors
                                                    .find(vErr => vErr.Type == "regex" && vErr.PropertyDefinition == propDefenition) == null;

                                                return modelIsValid;
                                            };
                                        }
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

                            if (dtoRules != null)
                                dtoRules.model = model;

                            if (defaultDtoViewModel != null) {

                                if (defaultDtoViewModel.model == null)
                                    defaultDtoViewModel.model = model;

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

                                model.propertyChanged.attach((sender, e) => {
                                    if (e.oldValue != e.newValue) {
                                        dtoViewModel.onMemberChanged(e.propertyName, e.newValue, e.oldValue);
                                    }
                                });

                                model.propertyChanging.attach((sender, e) => {
                                    if (e.oldValue != e.newValue) {
                                        dtoViewModel.onMemberChanging(e.propertyName, e.oldValue, e.newValue);
                                    }
                                });
                            }

                            if (dtoRules != null) {

                                model.propertyChanged.attach((sender, e) => {
                                    if (e.oldValue != e.newValue) {
                                        if (propModelControllers.find(p => p.$name == e.propertyName) != null)
                                            dtoRules.validateMember(e.propertyName, e.newValue, e.oldValue);
                                    }
                                });

                                dtoRules.setMemberValidaty = (memberName: string, errorKey: string, isValid: boolean): void => {
                                    const propModelCtrl = propModelControllers.find(p => p.$name == memberName);
                                    if (propModelCtrl != null)
                                        propModelCtrl.$setValidity(errorKey, isValid);
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