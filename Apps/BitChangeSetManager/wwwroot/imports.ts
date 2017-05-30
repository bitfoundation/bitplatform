/// <reference path="bower_components/bit-releases/foundation.core/foundation.core.d.ts" />
/// <reference path="bower_components/bit-releases/foundation.view/foundation.view.d.ts" />
/// <reference path="bower_components/bit-releases/foundation.viewmodel/foundation.viewmodel.d.ts" />

import FoundationCore = Foundation.Core;
import FoundationVM = Foundation.ViewModel;
import FoundationView = Foundation.View;

import SecureFormViewModelDependency = FoundationCore.SecureFormViewModelDependency;
import FormViewModelDependency = FoundationCore.FormViewModelDependency;
import DtoViewModelDependency = FoundationCore.DtoViewModelDependency;
import DtoRulesDependency = FoundationCore.DtoRulesDependency;
import ComponentDependency = Foundation.Core.ComponentDependency;
import DirectiveDependency = FoundationCore.DirectiveDependency;
import ObjectDependency = FoundationCore.ObjectDependency;
import Inject = FoundationCore.Inject;
import InjectAll = FoundationCore.InjectAll;
import IMessageReceiver = FoundationCore.Contracts.IMessageReceiver;
import ClientAppProfileManager = FoundationCore.ClientAppProfileManager;
import DependencyManager = FoundationCore.DependencyManager;
import Log = FoundationCore.Log;

import FormViewModel = FoundationVM.ViewModels.FormViewModel;
import DtoViewModel = FoundationVM.ViewModels.DtoViewModel;
import DtoFormController = FoundationVM.ViewModels.DtoFormController;
import DtoRules = FoundationVM.Implementations.DtoRules;
import Command = FoundationVM.Command;
import IEntityContextProvider = FoundationVM.Contracts.IEntityContextProvider;
