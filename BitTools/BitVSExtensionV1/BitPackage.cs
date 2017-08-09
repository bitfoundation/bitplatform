using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.HtmlClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using BitVSEditorUtils.HTML.Schema;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BitHtmlElement = BitVSEditorUtils.HTML.Schema.HtmlElement;
using Project = Microsoft.CodeAnalysis.Project;

namespace BitVSExtensionV1
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class BitPacakge : Package
    {
        private const string PackageGuidString = "F5222FDA-2C19-434B-9343-B0E942816E4C";
        private const string BitVSExtensionName = "Bit VS Extension V1";

        protected override void Initialize()
        {
            base.Initialize();

            _serviceContainer = this;

            _applicationObject = (DTE2)GetGlobalService(typeof(DTE));

            if (_applicationObject == null)
            {
                ShowInitialLoadProblem("applicationObject is null");
                return;
            }

            Window outputWindow = _applicationObject.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (outputWindow == null)
            {
                ShowInitialLoadProblem("outputWindow is null");
                return;
            }

            try
            {
                _outputWindow = (OutputWindow)outputWindow.Object;

                _outputPane = _outputWindow.OutputWindowPanes.Cast<OutputWindowPane>().ExtendedSingleOrDefault("Finding output pane", x => x.Name == BitVSExtensionName) ?? _outputWindow.OutputWindowPanes.Add(BitVSExtensionName);
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"Error finding output pane {ex}.");
                return;
            }

            try
            {
                _statusBar = (IVsStatusbar)_serviceContainer.GetService(typeof(SVsStatusbar));
                if (_statusBar == null)
                    throw new InvalidOperationException("status bar is null");
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"Could not find status bar {ex}.");
                return;
            }

            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));

            if (_componentModel == null)
            {
                LogWarn("Component model is null.");
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

            _applicationObject.Events.BuildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;
            _applicationObject.Events.BuildEvents.OnBuildDone += _buildEvents_OnBuildDone;
            _applicationObject.Events.BuildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;
        }

        private void GetWorkspace()
        {
            _visualStudioWorkspace = _componentModel.GetService<VisualStudioWorkspace>();

            if (_visualStudioWorkspace == null)
            {
                throw new InvalidOperationException("Visual studio workspace is null");
            }

            _visualStudioWorkspace.WorkspaceFailed += _workspace_WorkspaceFailed;
            _visualStudioWorkspace.WorkspaceChanged += _workspace_WorkspaceChanged;

            try
            {
                _msBuildWorkspace = MSBuildWorkspace.Create();
            }
            catch { }

            if (_msBuildWorkspace != null)
                _msBuildWorkspace.WorkspaceFailed += _workspace_WorkspaceFailed;
        }

        private void _workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            LogWarn($"{e.Diagnostic.Kind} {e.Diagnostic.Message}");
        }

        private void _workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                DoOnSolutionReadyOrChange();
            }
        }

        private void DoOnSolutionReadyOrChange()
        {
            if (!File.Exists(_visualStudioWorkspace.CurrentSolution.FilePath))
            {
                LogWarn("Could not find solution.");
                return;
            }

            if (!CurrentSolutionHasBitConfigV1JsonFile())
            {
                LogWarn("Could not find BitConfigV1.json file.");
                return;
            }

            if (Environment.Version < new Version("4.0.30319.42000"))
                ShowInitialLoadProblem("To develop bit projects, you've to install .NET 4.7.1 Developer Pack");

            try
            {
                _msBuildWorkspace?.OpenSolutionAsync(_visualStudioWorkspace.CurrentSolution.FilePath).GetAwaiter().GetResult();
            }
            catch { }

            _outputPane.Clear();

            DefaultBitConfigProvider configProvider = new DefaultBitConfigProvider();

            BitConfig config = null;

            try
            {
                config = configProvider.GetConfiguration(_visualStudioWorkspace.CurrentSolution, Enumerable.Empty<Project>().ToList());

                foreach (BitCodeGeneratorMapping mapping in config.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
                {
                    if (!_visualStudioWorkspace.CurrentSolution.Projects.Any(p => p.Name == mapping.DestinationProject.Name && p.Language == LanguageNames.CSharp))
                        throw new InvalidOperationException($"No project found named {mapping.DestinationProject.Name}");

                    foreach (BitTools.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (!_visualStudioWorkspace.CurrentSolution.Projects.Any(p => p.Name == proj.Name && p.Language == LanguageNames.CSharp))
                            throw new InvalidOperationException($"No project found named {proj.Name}");
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
                InitHtmlElements(config);
            }
            catch (Exception ex)
            {
                LogException("Init html elements failed.", ex);
            }

            _shouldGeneratedProjects = new List<Project> { };
            generateCodesForTheFirstTimeExecuted = false;
            thereWasAnErrorInBuild = false;
        }

        private bool CurrentSolutionHasBitConfigV1JsonFile()
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath) + "\\BitConfigV1.json"));
        }

        private void GenerateCodes(Workspace workspace)
        {
            Stopwatch sw = null;
            try
            {
                sw = Stopwatch.StartNew();

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()), dtosProvider
                    , new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                System.Threading.Tasks.Task.Run(async () => await generator.GenerateCodes(workspace.CurrentSolution, _shouldGeneratedProjects)).GetAwaiter().GetResult();

                Log($"Code Generation Completed in {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                LogException($"Code Generation failed in {sw.ElapsedMilliseconds} ms", ex);
            }
            finally
            {
                sw?.Stop();
            }
        }

        private void CleanCodes()
        {
            DefaultHtmlClientProxyCleaner cleaner = new DefaultHtmlClientProxyCleaner(new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()));

            System.Threading.Tasks.Task.Run(async () => await cleaner.DeleteCodes(_visualStudioWorkspace.CurrentSolution, _shouldGeneratedProjects)).GetAwaiter().GetResult();

            Log("Generated codes were deleted.");
        }

        private void InitHtmlElements(BitConfig config)
        {
            try
            {
                HtmlElementsContainer.Elements = new List<BitHtmlElement> { };

                List<BitHtmlElement> allElements = new List<BitHtmlElement>();

                foreach (string path in config.Schema.HtmlSchemaFiles)
                {
                    List<BitHtmlElement> newElements = JsonConvert.DeserializeObject<List<BitHtmlElement>>(File.ReadAllText(path));

                    foreach (BitHtmlElement newElement in newElements)
                    {
                        newElement.Attributes = newElement.Attributes ?? new List<HtmlAttribute> { };
                        newElement.Description = newElement.Description ?? "";

                        if (string.IsNullOrEmpty(newElement.Name))
                            throw new InvalidOperationException("Element must have a name");

                        newElement.Type = newElement.Type ?? "";

                        BitHtmlElement equivalentHtmlElement = allElements.FirstOrDefault(e => e.Name == newElement.Name);

                        if (equivalentHtmlElement != null)
                            equivalentHtmlElement.Attributes.AddRange(newElement.Attributes);
                        else
                            allElements.Add(newElement);
                    }
                }

                if (!allElements.Any(element => element.Name == "*"))
                    allElements.Add(new BitHtmlElement { Name = "*", Attributes = new List<HtmlAttribute> { }, Description = "", Type = "existing" });

                HtmlElementsContainer.Elements = allElements;
            }
            catch (Exception ex)
            {
                LogException("Init html elements failed.", ex);
            }
        }

        private void _buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (!CurrentSolutionHasBitConfigV1JsonFile())
                return;

            if (generateCodesForTheFirstTimeExecuted == false || thereWasAnErrorInBuild == true)
            {
                _shouldGeneratedProjects = _visualStudioWorkspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList();
                try
                {
                    if (_msBuildWorkspace != null)
                        GenerateCodes(_msBuildWorkspace);
                    else
                        throw new InvalidOperationException("MsBuildWorkspace is null");
                }
                catch
                {
                    GenerateCodes(_visualStudioWorkspace);
                }
            }

            generateCodesForTheFirstTimeExecuted = true;
            thereWasAnErrorInBuild = false;

            _shouldGeneratedProjects.Clear();
        }

        private void _buildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (!CurrentSolutionHasBitConfigV1JsonFile())
                return;

            thereWasAnErrorInBuild = thereWasAnErrorInBuild && success;

            Project proj = _visualStudioWorkspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                                .ExtendedSingle($"Lookin for {project} in [ {(string.Join(",", _visualStudioWorkspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).Select(prj => prj.Name)))} ]", prj => Path.GetFileName(prj.FilePath) == project.Split('\\').Last());

            _shouldGeneratedProjects.Add(proj);
        }

        private void _buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (!CurrentSolutionHasBitConfigV1JsonFile())
                return;

            try
            {
                if (action == vsBuildAction.vsBuildActionClean)
                {
                    CleanCodes();
                }
                else
                {
                    GenerateCodes(_visualStudioWorkspace);
                }
            }
            catch (Exception ex)
            {
                LogException("Generate|Clean codes failed.", ex);
            }
        }

        private void LogException(string text, Exception ex)
        {
            _statusBar.SetText($"Bit: {text} See output pane for more info");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n {ex} \n");
            _outputPane.Activate();
        }

        private void Log(string text)
        {
            _statusBar.SetText($"Bit: {text}");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n");
        }

        private void LogWarn(string text)
        {
            _statusBar.SetText($"Bit: {text}");
            _outputPane.OutputString($"{text} {DateTimeOffset.Now} \n");
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
                _visualStudioWorkspace.WorkspaceChanged -= _workspace_WorkspaceChanged;
                _visualStudioWorkspace.WorkspaceFailed -= _workspace_WorkspaceFailed;
            }

            if (_msBuildWorkspace != null)
            {
                _msBuildWorkspace.WorkspaceFailed -= _workspace_WorkspaceFailed;
                _msBuildWorkspace.Dispose();
            }

            if (_applicationObject?.Events?.BuildEvents != null)
            {
                _applicationObject.Events.BuildEvents.OnBuildProjConfigDone -= _buildEvents_OnBuildProjConfigDone;

                _applicationObject.Events.BuildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;

                _applicationObject.Events.BuildEvents.OnBuildDone -= _buildEvents_OnBuildDone;
            }

            base.Dispose(disposing);
        }

        private List<Project> _shouldGeneratedProjects;

        private VisualStudioWorkspace _visualStudioWorkspace;

        private MSBuildWorkspace _msBuildWorkspace;

        private IComponentModel _componentModel;

        private IServiceContainer _serviceContainer;

        private OutputWindow _outputWindow;

        private OutputWindowPane _outputPane;

        private DTE2 _applicationObject;

        private IVsStatusbar _statusBar;

        private bool generateCodesForTheFirstTimeExecuted;

        private bool thereWasAnErrorInBuild;
    }
}
