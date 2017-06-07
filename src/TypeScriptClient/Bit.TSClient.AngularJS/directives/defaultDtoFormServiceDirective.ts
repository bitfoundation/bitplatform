module Bit.Directives {

    @DirectiveDependency({
        name: "DtoFormService",
        restrict: "A",
        scope: false
    })
    export class DefaultDtoFormServiceDirective {

        public ngModel: ng.INgModelController & { $$parentForm: ViewModels.IDtoFormController };

        public constructor( @Inject("$scope") public $scope: ng.IScope,
            @Inject("$attrs") public $attrs: ng.IAttributes,
            @Inject("$element") public $element: JQuery,
            @Inject("$parse") public $parse: ng.IParseService,
            @Inject("DateTimeService") public dateTimeService: Contracts.IDateTimeService) {

        }

        public dtoViewModel: ViewModels.DtoViewModel<Model.Contracts.IDto, Implementations.DtoRules<Model.Contracts.IDto>> = null;
        public dtoRules: Implementations.DtoRules<Model.Contracts.IDto> = null;
        public perDtoFormStorage: {
            prevValidationsRollbackHandlers?: Array<{ memberName: string, errorKey: string, handler: () => void }>,
            dtoViewModelPropertyChangedFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void,
            dtoViewModelPropertyChangingFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void,
            dtoRulesPropertyChangedFunction?: (sender?: $data.Entity, e?: $data.Event & { newValue: any, oldValue: any, propertyName: any }) => void
        } = {};

        public cleanUp() {

            if (this.dtoViewModel != null) {

                if (this.dtoViewModel.model != null && this.dtoViewModel.form != null) {
                    if (this.perDtoFormStorage.dtoViewModelPropertyChangedFunction != null) {
                        try {
                            this.dtoViewModel.model.propertyChanged.detach(this.perDtoFormStorage.dtoViewModelPropertyChangedFunction);
                        }
                        catch (e) { }
                    }
                    if (this.perDtoFormStorage.dtoViewModelPropertyChangingFunction != null) {
                        try {
                            this.dtoViewModel.model.propertyChanging.detach(this.perDtoFormStorage.dtoViewModelPropertyChangingFunction);
                        }
                        catch (e) { }
                    }

                    this.dtoViewModel.form = null;
                }

            }

            if (this.dtoRules != null) {

                if (this.dtoRules.model != null && this.perDtoFormStorage.dtoRulesPropertyChangedFunction != null) {

                    try {
                        this.dtoRules.model.propertyChanged.detach(this.perDtoFormStorage.dtoRulesPropertyChangedFunction);
                    }
                    catch (e) { }

                    if (this.perDtoFormStorage.prevValidationsRollbackHandlers != null) {

                        for (let prevValidationsRollbackHandler of this.perDtoFormStorage.prevValidationsRollbackHandlers) {
                            prevValidationsRollbackHandler.handler();
                        }

                        this.perDtoFormStorage.prevValidationsRollbackHandlers = null;
                    }

                }
            }

            this.perDtoFormStorage = {};

        }

        @Command()
        public async $onInit(): Promise<void> {

            this.ngModel = this.$element.data('$ngModelController');

            const clientAppProfile = ClientAppProfileManager.getCurrent().getClientAppProfile();

            this.$scope.$watch(this.$attrs.ngModel, (newModel: any, oldModel: any) => {

                if (newModel == null && oldModel == null)
                    return;

                if (newModel == oldModel)
                    oldModel = null;

                if (newModel != null && newModel.innerInstance != null) {
                    newModel = newModel.innerInstance();
                    this.$parse(this.$attrs.ngModel).assign(this.$scope, newModel);
                }

                if (newModel != null && !(newModel instanceof $data.Entity))
                    throw new Error("Can't validate non-entity | non-dto models");

                let modelType = newModel != null ? newModel.getType() : oldModel.getType();

                let memberDefenitions = modelType.memberDefinitions;

                let propModelControllers = [];

                this.ngModel.$$parentForm.isValid = (): boolean => {
                    let isValid = true;
                    propModelControllers.forEach(p => {
                        if (this.dtoRules != null && newModel != null)
                            this.dtoRules.validateMember(p.$name, newModel[p.$name], newModel[p.$name]);
                        p.$validate();
                        isValid = isValid && p.$valid;
                    });
                    return isValid;
                };

                this.ngModel.$$parentForm.$setPristine();
                this.ngModel.$$parentForm.$setUntouched();

                for (let prp in this.ngModel.$$parentForm) {

                    if (this.ngModel.$$parentForm.hasOwnProperty(prp)) {

                        let propDefenition = memberDefenitions[`$${prp}`];

                        if (propDefenition != null && propDefenition.kind == "property") {

                            let propModelController = this.ngModel.$$parentForm[prp];

                            Object.defineProperty(propModelController, "visible", {
                                configurable: true,
                                set: (isVisible: boolean) => {
                                    let currentItem = angular.element(this.$element).find(`[name='${propDefenition.name}']`);
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
                                    let currentItem = angular.element(this.$element).find(`[name='${propDefenition.name}']`);
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

                            if (propModelController["isFirstTimeIndicator"] == null) {

                                propModelController["isFirstTimeIndicator"] = {};

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
                                            viewValue = this.dateTimeService.parseDate(viewValue);
                                        }
                                        return viewValue;
                                    });
                                }
                                if (propDefenition.originalType == "Edm.Decimal" || propDefenition.originalType == "Edm.Double" || propDefenition.originalType == "Edm.Single") {
                                    propModelController.$parsers.push((viewValue) => {
                                        if (viewValue != null && typeof viewValue == "string") {
                                            let viewValueAsString: string = viewValue;
                                            if (viewValueAsString.startsWith(".")) {
                                                viewValueAsString = `0${viewValueAsString}`;
                                            }
                                            if (viewValueAsString.endsWith(".")) {
                                                viewValueAsString = viewValueAsString + "0";
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

                if (this.$attrs.dtoViewModel != null) {
                    this.dtoViewModel = this.$parse(this.$attrs.dtoViewModel)(this.$scope);
                }
                else {
                    let tryToGetDtoViewModel = this.$parse("vm")(this.$scope);
                    if (tryToGetDtoViewModel instanceof ViewModels.DtoViewModel)
                        this.dtoViewModel = tryToGetDtoViewModel as ViewModels.DtoViewModel<Model.Contracts.IDto, Implementations.DtoRules<Model.Contracts.IDto>>;
                }

                if (this.$attrs.dtoRules != null)
                    this.dtoRules = this.$parse(this.$attrs.dtoRules)(this.$scope);
                else if (this.dtoViewModel != null)
                    this.dtoRules = this.dtoViewModel.rules;

                if (oldModel != null)
                    this.cleanUp();

                if (this.dtoRules != null) {

                    if (!(this.dtoRules instanceof Implementations.DtoRules)) {
                        throw new Error(`dto rules is not instance of dto rules`);
                    }

                    if (this.dtoRules.model == null || this.dtoRules.model == oldModel)
                        this.dtoRules.model = newModel;

                    if (this.perDtoFormStorage.dtoRulesPropertyChangedFunction == null) {
                        this.perDtoFormStorage.dtoRulesPropertyChangedFunction = (sender, e) => {
                            if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                this.dtoRules.validateMember(e.propertyName, e.newValue, e.oldValue);
                            }
                        };
                    }

                    if (newModel != null)
                        newModel.propertyChanged.attach(this.perDtoFormStorage.dtoRulesPropertyChangedFunction);

                    this.perDtoFormStorage.prevValidationsRollbackHandlers = [];

                    this.dtoRules.setMemberValidaty = (memberName: string, errorKey: string, isValid: boolean): void => {
                        const propModelCtrl = propModelControllers.find(p => p.$name == memberName);
                        if (propModelCtrl != null) {
                            propModelCtrl.$setValidity(errorKey, isValid);
                            if (isValid == false) {
                                if (!this.perDtoFormStorage.prevValidationsRollbackHandlers.some(h => h.memberName == memberName && h.errorKey == errorKey)) {
                                    this.perDtoFormStorage.prevValidationsRollbackHandlers.push({
                                        memberName: memberName, errorKey: errorKey, handler: () => {
                                            propModelCtrl.$setValidity(errorKey, true);
                                            propModelCtrl.validityEvaludated = false;
                                        }
                                    });
                                }
                            }
                            else {
                                this.perDtoFormStorage.prevValidationsRollbackHandlers = this.perDtoFormStorage.prevValidationsRollbackHandlers.filter(h => h.memberName != memberName || h.errorKey != errorKey);
                            }
                        }
                        else {
                            if (clientAppProfile.isDebugMode == true)
                                console.warn(`No Prop named ${memberName} is in dto form`);
                        }
                    };

                    (async () => {
                        try {
                            await this.dtoRules.onActivated();
                        }
                        finally {
                            ScopeManager.update$scope(this.$scope);
                        }
                    })();
                }

                if (this.dtoViewModel != null) {

                    if (!(this.dtoViewModel instanceof ViewModels.DtoViewModel)) {
                        throw new Error(`dto view model ${this.$attrs.dtoViewModel} is not instance of dto view model`);
                    }

                    if (this.dtoViewModel.model == null || this.dtoViewModel.model == oldModel)
                        this.dtoViewModel.model = newModel;

                    this.dtoViewModel.form = this.ngModel.$$parentForm as ViewModels.DtoFormController<Model.Contracts.IDto>;

                    if (this.perDtoFormStorage.dtoViewModelPropertyChangedFunction == null) {
                        this.perDtoFormStorage.dtoViewModelPropertyChangedFunction = (sender, e) => {
                            if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                this.dtoViewModel.onMemberChanged(e.propertyName, e.newValue, e.oldValue);
                            }
                        };
                    }

                    if (this.perDtoFormStorage.dtoViewModelPropertyChangingFunction == null) {
                        this.perDtoFormStorage.dtoViewModelPropertyChangingFunction = (sender, e) => {
                            if (e.oldValue != e.newValue && e.propertyName != "ValidationErrors") {
                                this.dtoViewModel.onMemberChanging(e.propertyName, e.newValue, e.oldValue);
                            }
                        };
                    }

                    if (newModel != null) {
                        newModel.propertyChanged.attach(this.perDtoFormStorage.dtoViewModelPropertyChangedFunction);
                        newModel.propertyChanging.attach(this.perDtoFormStorage.dtoViewModelPropertyChangingFunction);
                    }

                    (async () => {
                        try {
                            await this.dtoViewModel.onActivated();
                        }
                        finally {
                            ScopeManager.update$scope(this.$scope);
                        }
                    })();
                }

                if (this.dtoRules != null && this.dtoViewModel != null && this.dtoRules.model != this.dtoViewModel.model)
                    throw new Error("Dto rules and forms are not using the same model instance");
                if (this.dtoRules != null && this.dtoRules.model != newModel)
                    throw new Error("dto rules's model is not using the same instance of ng-model of dto-form");
                if (this.dtoViewModel != null && this.dtoViewModel.model != newModel)
                    throw new Error("dto view models's model is not using the same instance of ng-model of dto-form");
            });
        }

        public $onDestroy() {
            this.cleanUp();
        }

    }
}