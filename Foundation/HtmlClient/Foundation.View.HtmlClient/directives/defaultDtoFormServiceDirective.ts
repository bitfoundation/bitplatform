module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'dtoFormService' })
    export class DefaultDtoFormServiceDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                restrict: 'A',
                require: 'ngModel',
                scope: false,
                link($scope: angular.IScope, element: JQuery, attributes: any, ctrl: any, ngModel) {

                    let dtoViewModel: Foundation.ViewModel.ViewModels.DtoViewModel<Foundation.Model.Contracts.IDto, ViewModel.ViewModels.DtoRules<Model.Contracts.IDto>> = null;

                    let dependencyManager = Core.DependencyManager.getCurrent();

                    let $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");
                    let $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");

                    $timeout(() => {

                        let unRegister = $scope.$watch(attributes.ngModel, (model: any) => {

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
                                    if (dtoViewModel != null && dtoViewModel.rules != null)
                                        dtoViewModel.rules.validateMember(p.$name, model[p.$name], model[p.$name]);
                                    p.$validate();
                                    isValid = isValid && p.$valid;
                                });
                                return isValid;
                            };

                            ctrl.$$parentForm.visible = (propName: string, isVisible?: boolean): boolean => {

                                if (isVisible == null)
                                    throw new Error("Return prop is visible or not is not implemented yet");

                                let currentForm = element;
                                let currentItem = angular.element(element).find(`[name='${propName}']`);
                                let data = currentItem.data();
                                if (data != null && data["handler"] != null && data["handler"].wrapper != null)
                                    currentItem = data["handler"].wrapper;

                                if (isVisible == true) {
                                    currentItem.show();
                                    currentItem.parents('md-input-container').show();
                                }
                                else {
                                    currentItem.hide();
                                    currentItem.parents('md-input-container').hide();
                                }

                                return true;
                            };

                            ctrl.$$parentForm.editable = (propName: string, isEditable?: boolean): boolean => {

                                if (isEditable == null)
                                    throw new Error("Return prop is editable or not is not implemented yet");

                                let currentForm = element;
                                let currentItem = angular.element(element).find(`[name='${propName}']`);
                                let data = currentItem.data();
                                if (data != null && data["handler"] != null && data["handler"].wrapper != null) {
                                    currentItem = data["handler"].wrapper;
                                    if (data["handler"]["readonly"] != null)
                                        data["handler"]["readonly"](!isEditable);
                                }

                                currentItem.prop('readonly', !isEditable);

                                return true;
                            };

                            let propModelControllers = [];

                            for (let prp in ctrl.$$parentForm) {

                                if (ctrl.$$parentForm.hasOwnProperty(prp)) {

                                    let propDefenition = memberDefenitions[`$${prp}`];

                                    if (propDefenition != null && propDefenition.kind == 'property') {

                                        let propModelController = ctrl.$$parentForm[prp];

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

                                        if (propDefenition.dataType == "Edm.DateTimeOffset" || propDefenition.dataType == $data['DateTimeOffset']) {
                                            propModelController.$parsers.push((viewValue) => {
                                                if (viewValue != null && !(viewValue instanceof Date)) {
                                                    viewValue = kendo.parseDate(viewValue);
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
                            }

                            if (dtoViewModel != null) {

                                if (dtoViewModel.model == null)
                                    dtoViewModel.model = model;

                                if (dtoViewModel.form == null)
                                    dtoViewModel.form = ctrl.$$parentForm;

                                dtoViewModel.model.propertyChanged.attach((sender, e) => {
                                    if (e.oldValue != e.newValue) {
                                        dtoViewModel.onMemberChanged(e.propertyName, e.newValue, e.oldValue);
                                        if (dtoViewModel.rules != null && propModelControllers.find(p => p.$name == e.propertyName) != null)
                                            dtoViewModel.rules.validateMember(e.propertyName, e.newValue, e.oldValue);
                                    }
                                });

                                dtoViewModel.model.propertyChanging.attach((sender, e) => {
                                    if (e.oldValue != e.newValue) {
                                        dtoViewModel.onMemberChanging(e.propertyName, e.oldValue, e.newValue);
                                    }
                                });

                                if (dtoViewModel.rules != null) {
                                    if (dtoViewModel.rules.model == null)
                                        dtoViewModel.rules.model = model;
                                    dtoViewModel.rules.setMemberValidaty = (memberName: string, errorKey: string, isValid: boolean): void => {
                                        let propModelCtrl = propModelControllers.find(p => p.$name == memberName);
                                        if (propModelCtrl != null)
                                            propModelCtrl.$setValidity(errorKey, isValid);
                                        else {
                                            if (Foundation.Core.ClientAppProfileManager.getCurrent().clientAppProfile.isDebugMode == true)
                                                console.warn(`No Prop named ${memberName} is in dto form`);
                                        }
                                    };
                                }

                                (async function () {
                                    try {
                                        await dtoViewModel.onActivated();
                                    }
                                    finally {
                                        Foundation.ViewModel.ScopeManager.update$scope($scope);
                                    }
                                })();
                            }

                        });
                    });
                }
            });
        }
    }
}