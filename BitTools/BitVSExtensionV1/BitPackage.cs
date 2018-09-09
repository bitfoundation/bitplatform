using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace BitVSExtensionV1
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class BitPacakge : AsyncPackage
    {
        private const string PackageGuidString = "F5222FDA-2C19-434B-9343-B0E942816E4C";
        private const string BitVSExtensionName = "Bit VS Extension V1";

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _ayncServiceProvider = (IAsyncServiceProvider)(await GetServiceAsync(typeof(SAsyncServiceProvider)));

            _applicationObject = (DTE2)GetGlobalService(typeof(DTE));

            if (_applicationObject == null)
            {
                ShowInitialLoadProblem($"{_applicationObject}-{nameof(DTE2)} not found");
                return;
            }

            Window outputWindow = _applicationObject.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (outputWindow == null)
            {
                ShowInitialLoadProblem($"{outputWindow}-{nameof(Window)} not found");
                return;
            }

            try
            {
                _outputWindow = (OutputWindow)outputWindow.Object;

                _outputPane = _outputWindow.OutputWindowPanes.Cast<OutputWindowPane>().ExtendedSingleOrDefault("Finding output pane", x => x.Name == BitVSExtensionName) ?? _outputWindow.OutputWindowPanes.Add(BitVSExtensionName);
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"{_outputPane}-{nameof(OutputWindowPane)} not found => {ex}.");
                return;
            }

            try
            {
                _statusBar = (IVsStatusbar)(await _ayncServiceProvider.GetServiceAsync(typeof(SVsStatusbar)));
                if (_statusBar == null)
                    throw new InvalidOperationException("status bar is null");
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"{_statusBar}-{nameof(IVsStatusbar)} not found => {ex}");
                return;
            }

            _componentModel = (IComponentModel)(await GetServiceAsync(typeof(SComponentModel)));

            if (_componentModel == null)
            {
                LogWarn($"{_componentModel}-{nameof(IComponentModel)} is null.");
                return;
            }

            try
            {
                GetWorkspace();
            }
            catch (Exception ex)
            {
                LogException("Workspace not found.", ex);
                return;
            }

            _buildEvents = _applicationObject.Events.BuildEvents;
            _solutionEvents = _applicationObject.Events.SolutionEvents;
            _buildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;
            _buildEvents.OnBuildDone += _buildEvents_OnBuildDone;
            _buildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;
            _solutionEvents.Opened += RunOnEitherSolutionOpenOrFirstPackageInitialize;

            RunOnEitherSolutionOpenOrFirstPackageInitialize();
        }

        private bool _RunOnEitherSolutionOpenOrFirstPackageInitializeIsBeingExecuted = false;

        private async void RunOnEitherSolutionOpenOrFirstPackageInitialize()
        {
            try
            {
                if (_RunOnEitherSolutionOpenOrFirstPackageInitializeIsBeingExecuted == true)
                    return;

                _RunOnEitherSolutionOpenOrFirstPackageInitializeIsBeingExecuted = true;

                bool vsHasASavedSolutionButRoslynWorkspaceHasNoPath()
                {
                    bool result = _applicationObject.Solution.Saved && string.IsNullOrEmpty(_visualStudioWorkspace.CurrentSolution.FilePath);
                    if (result == false)
                        LogWarn("Visual studio has a saved solution, but roslyn's workspace has no file path");
                    return result;
                }

                bool vsSolutionHasSomeProjectsButRoslynOneNothing()
                {
                    bool result = _applicationObject.Solution.Projects.Cast<object>().Any() && !_visualStudioWorkspace.CurrentSolution.Projects.Any();
                    if (result == false)
                        LogWarn("Visual studio has some projects, but roslyn's workspace has no project");
                    return result;
                }

                int retryCount = 30;

                while ((vsHasASavedSolutionButRoslynWorkspaceHasNoPath() || vsSolutionHasSomeProjectsButRoslynOneNothing()) && retryCount > 0)
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    retryCount--;
                }

                if (vsHasASavedSolutionButRoslynWorkspaceHasNoPath() || vsSolutionHasSomeProjectsButRoslynOneNothing())
                    LogWarn("15 seconds delay wasn't enough to make Visual Studio's workspace ready.");

                await DoOnSolutionReadyOrChange();
            }
            finally
            {
                _RunOnEitherSolutionOpenOrFirstPackageInitializeIsBeingExecuted = false;
            }
        }

        private void GetWorkspace()
        {
            _visualStudioWorkspace = _componentModel.GetService<VisualStudioWorkspace>();

            if (_visualStudioWorkspace == null)
            {
                throw new InvalidOperationException("Visual studio workspace is null");
            }

            _visualStudioWorkspace.WorkspaceFailed += _workspace_WorkspaceFailed;
        }

        private void _workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            LogWarn($"{sender.GetType().Name} {e.Diagnostic.Kind} {e.Diagnostic.Message}");
        }

        private async System.Threading.Tasks.Task DoOnSolutionReadyOrChange()
        {
            if (!File.Exists(_visualStudioWorkspace.CurrentSolution.FilePath))
            {
                LogWarn("Could not find solution.");
                return;
            }

            if (!WorkspaceHasBitConfigV1JsonFile())
            {
                LogWarn("Could not find BitConfigV1.json file.");
                return;
            }

            _outputPane.OutputString("__________----------__________ \n");

            DefaultBitConfigProvider configProvider = new DefaultBitConfigProvider();

            try
            {
                BitConfig config = configProvider.GetConfiguration(_visualStudioWorkspace);

                foreach (BitCodeGeneratorMapping mapping in config.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
                {
                    if (!_visualStudioWorkspace.CurrentSolution.Projects.Any(p => p.Name == mapping.DestinationProject.Name && p.Language == LanguageNames.CSharp))
                        LogWarn($"No project found named {mapping.DestinationProject.Name}");

                    foreach (BitTools.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (!_visualStudioWorkspace.CurrentSolution.Projects.Any(p => p.Name == proj.Name && p.Language == LanguageNames.CSharp))
                            LogWarn($"No project found named {proj.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException("Parse BitConfigV1.json failed.", ex);
                return;
            }

            try
            {
                bitWorkspaceIsPrepared = thereWasAnErrorInLastBuild = lastActionWasClean = false;

                Log("Preparing bit workspace... This includes restoring nuget packages, building your solution and generating codes.");

                _applicationObject.Solution.SolutionBuild.Build(WaitForBuildToFinish: true);
            }
            catch (Exception ex)
            {
                LogException("Bit workspace preparation failed", ex);
            }
            finally
            {
                bitWorkspaceIsPrepared = true;
                await CallGenerateCodes();
                Log("Bit workspace gets prepared", activatePane: true);
            }
        }

        private bool WorkspaceHasBitConfigV1JsonFile()
        {
            return File.Exists(_visualStudioWorkspace.CurrentSolution.FilePath) && File.Exists(Path.Combine(Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath) + "\\BitConfigV1.json"));
        }

        private async System.Threading.Tasks.Task CallGenerateCodes()
        {
            if (bitWorkspaceIsPrepared == false)
            {
                return;
            }

            Stopwatch sw = null;

            try
            {
                sw = Stopwatch.StartNew();

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new DefaultBitConfigProvider(), dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                Workspace workspaceForCodeGeneration = await GetWorkspaceForCodeGeneration();

                try
                {
                    await generator.GenerateCodes(workspaceForCodeGeneration);

                    Log($"Code Generation Completed in {sw.ElapsedMilliseconds} ms using {workspaceForCodeGeneration.GetType().Name}");
                }
                finally
                {
                    if (!(workspaceForCodeGeneration is VisualStudioWorkspace))
                        workspaceForCodeGeneration.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogException("Code Generation failed.", ex);
            }
            finally
            {
                sw?.Stop();
            }
        }

        private async System.Threading.Tasks.Task<Workspace> GetWorkspaceForCodeGeneration()
        {
            bool vsWorkspaceIsValid = true;

            foreach (Microsoft.CodeAnalysis.Project proj in _visualStudioWorkspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp))
            {
                if (!(await proj.GetCompilationAsync()).ReferencedAssemblyNames.Any())
                {
                    LogWarn($"{nameof(VisualStudioWorkspace)} is not ready due project {proj.Name}");
                    vsWorkspaceIsValid = false;
                    break;
                }
            }

            if (vsWorkspaceIsValid)
                return _visualStudioWorkspace;

            return await CreateMsBuildWorkspace();
        }

        private async System.Threading.Tasks.Task<Workspace> CreateMsBuildWorkspace()
        {
            MSBuildWorkspace msBuildWorkspaace = MSBuildWorkspace.Create(new Dictionary<string, string>(), _visualStudioWorkspace.Services.HostServices);
            msBuildWorkspaace.LoadMetadataForReferencedProjects = msBuildWorkspaace.SkipUnrecognizedProjects = true;
            await msBuildWorkspaace.OpenSolutionAsync(_visualStudioWorkspace.CurrentSolution.FilePath);
            return msBuildWorkspaace;
        }

        private async System.Threading.Tasks.Task CallCleanCodes()
        {
            DefaultTypeScriptClientProxyCleaner cleaner = new DefaultTypeScriptClientProxyCleaner(new DefaultBitConfigProvider());

            await cleaner.DeleteCodes(_visualStudioWorkspace);

            Log("Generated codes were deleted.");
        }

        private async void _buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (!WorkspaceHasBitConfigV1JsonFile() || action == vsBuildAction.vsBuildActionClean)
                return;

            if (thereWasAnErrorInLastBuild == true || lastActionWasClean == true)
            {
                lastActionWasClean = false;
                thereWasAnErrorInLastBuild = false;
                await CallGenerateCodes();
            }
        }

        private void _buildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (!WorkspaceHasBitConfigV1JsonFile())
                return;

            thereWasAnErrorInLastBuild = thereWasAnErrorInLastBuild && success;
        }

        private async void _buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (!WorkspaceHasBitConfigV1JsonFile())
                return;

            try
            {
                if (action == vsBuildAction.vsBuildActionClean)
                {
                    await CallCleanCodes();
                    lastActionWasClean = true;
                }
                else
                {
                    await CallGenerateCodes();
                }
            }
            catch (Exception ex)
            {
                LogException("Generate|Clean codes failed.", ex);
            }
        }

        private void LogException(string text, Exception ex, bool activatePane = true)
        {
            _statusBar.SetText($"Bit: {text} See output pane for more info");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n {ex} \n");
            _outputPane.Activate();
            if (activatePane == true)
                _outputPane.Activate();
        }

        private void Log(string text, bool activatePane = false)
        {
            _statusBar.SetText($"Bit: {text}");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n");
            if (activatePane == true)
                _outputPane.Activate();
        }

        private void LogWarn(string text, bool activatePane = false)
        {
            _statusBar.SetText($"Bit: {text}");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n");
            if (activatePane == true)
                _outputPane.Activate();
        }

        private void ShowInitialLoadProblem(string errorMessage)
        {
            if (errorMessage == null)
                throw new ArgumentNullException(nameof(errorMessage));

            if (string.IsNullOrEmpty(errorMessage))
                throw new ArgumentException(nameof(errorMessage));

            MessageBox.Show(errorMessage, BitVSExtensionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override void Dispose(bool disposing)
        {
            if (_visualStudioWorkspace != null)
            {
                _visualStudioWorkspace.WorkspaceFailed -= _workspace_WorkspaceFailed;
            }

            try
            {
                if (_buildEvents != null)
                {
                    _buildEvents.OnBuildProjConfigDone -= _buildEvents_OnBuildProjConfigDone;

                    _buildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;

                    _buildEvents.OnBuildDone -= _buildEvents_OnBuildDone;
                }
                if (_solutionEvents != null)
                {
                    _solutionEvents.Opened -= RunOnEitherSolutionOpenOrFirstPackageInitialize;
                }
            }
            catch { }

            base.Dispose(disposing);
        }

        private VisualStudioWorkspace _visualStudioWorkspace;

        private IComponentModel _componentModel;

        private IAsyncServiceProvider _ayncServiceProvider;

        private OutputWindow _outputWindow;

        private OutputWindowPane _outputPane;

        private DTE2 _applicationObject;

        private BuildEvents _buildEvents;

        private SolutionEvents _solutionEvents;

        private IVsStatusbar _statusBar;

        private bool lastActionWasClean;

        private bool thereWasAnErrorInLastBuild;

        private bool bitWorkspaceIsPrepared;
    }
}
