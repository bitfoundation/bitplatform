/// <reference path="bower_components/bit-html-client/foundation.core/foundation.core.d.ts" />
/// <reference path="bower_components/bit-html-client/foundation.view/foundation.view.d.ts" />
/// <reference path="bower_components/bit-html-client/foundation.viewmodel/foundation.viewmodel.d.ts" />

import FoundationCore = Foundation.Core;
import FoundationVM = Foundation.ViewModel;
import FoundationView = Foundation.View;

import FormViewModelDependency = FoundationCore.FormViewModelDependency;
import DtoViewModelDependency = FoundationCore.DtoViewModelDependency;
import DtoRulesDependency = FoundationCore.DtoRulesDependency;
import Inject = FoundationCore.Inject;
import IMessageReceiver = FoundationCore.Contracts.IMessageReceiver;
import ObjectDependency = FoundationCore.ObjectDependency;
import ClientAppProfileManager = FoundationCore.ClientAppProfileManager;
import DependencyManager = FoundationCore.DependencyManager;

import FormViewModel = FoundationVM.ViewModels.FormViewModel;
import SecureViewModel = FoundationVM.ViewModels.SecureFormViewModel;
import DtoViewModel = FoundationVM.ViewModels.DtoViewModel;
import DtoRules = FoundationVM.Implementations.DtoRules;
import Command = FoundationVM.Command;
import IEntityContextProvider = FoundationVM.Contracts.IEntityContextProvider;
