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

import FormViewModel = FoundationVM.ViewModels.FormViewModel;
import DtoViewModel = FoundationVM.ViewModels.DtoViewModel;
import DtoRules = FoundationVM.Implementations.DtoRules;
import Command = FoundationVM.Command;
import IEntityContextProvider = FoundationVM.Contracts.IEntityContextProvider;
