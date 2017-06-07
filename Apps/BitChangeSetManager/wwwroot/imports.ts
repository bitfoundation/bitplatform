/// <reference path="bower_components/bit-releases/typescript-client/typings.d.ts" />

import SecureFormViewModelDependency = Bit.SecureFormViewModelDependency;
import FormViewModelDependency = Bit.FormViewModelDependency;
import DtoViewModelDependency = Bit.DtoViewModelDependency;
import DtoRulesDependency = Bit.DtoRulesDependency;
import ComponentDependency = Bit.ComponentDependency;
import DirectiveDependency = Bit.DirectiveDependency;
import ObjectDependency = Bit.ObjectDependency;
import Inject = Bit.Inject;
import InjectAll = Bit.InjectAll;
import IMessageReceiver = Bit.Contracts.IMessageReceiver;
import ClientAppProfileManager = Bit.ClientAppProfileManager;
import DependencyManager = Bit.DependencyManager;
import Log = Bit.Log;

import FormViewModel = Bit.ViewModels.FormViewModel;
import DtoViewModel = Bit.ViewModels.DtoViewModel;
import DtoFormController = Bit.ViewModels.DtoFormController;
import DtoRules = Bit.Implementations.DtoRules;
import Command = Bit.Command;
import IEntityContextProvider = Bit.Contracts.IEntityContextProvider;
