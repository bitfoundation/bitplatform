using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.HtmlClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using BitVSEditorUtils.HTML.Schema;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BitHtmlElement = BitVSEditorUtils.HTML.Schema.HtmlElement;
using Project = Microsoft.CodeAnalysis.Project;
using Task = System.Threading.Tasks.Task;

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

        protected override async void Initialize()
        {
            base.Initialize();

            _serviceContainer = this;

            _applicationObject = (DTE2)GetGlobalService(typeof(DTE));

            if (_applicationObject == null)
            {
                ShowInitialLoadProblem("applicationObject is null");
                return;
            }

            string vsVersion = _applicationObject.Version;

            if (vsVersion == "14.0" /*VS 2015*/)
            {
                new[] { "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.Common", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.VisualBasic", "Microsoft.CodeAnalysis.VisualBasic.Workspaces", "Microsoft.CodeAnalysis.Workspaces", "Microsoft.VisualStudio.LanguageServices", "Microsoft.CodeAnalysis.Features", "Microsoft.CodeAnalysis.CSharp.Features", "Microsoft.CodeAnalysis.VisualBasic.Features" }.ToList()
                    .ForEach(needsRuntimeAssemblyRedirectInVS2015 =>
                    {
                        RedirectAssembly(needsRuntimeAssemblyRedirectInVS2015, new Version("1.3.1.0"), "31bf3856ad364e35");
                    });

                RedirectAssembly("System.Collections.Immutable", new Version("1.1.37"), "b03f5f7f11d50a3a");

                new[] { "Microsoft.Web.Core", "Microsoft.Html.Core", "Microsoft.Html.Editor", "Microsoft.Web.Editor", "Microsoft.VisualStudio.Language.Intellisense", "Microsoft.VisualStudio.CoreUtility", "Microsoft.VisualStudio.Imaging", "Microsoft.VisualStudio.Text.Data", "Microsoft.VisualStudio.Text.Logic", "Microsoft.VisualStudio.Text.UI", "Microsoft.VisualStudio.Threading", "Microsoft.VisualStudio.Utilities", "Microsoft.VisualStudio.Validation" }.ToList()
                    .ForEach(needsRuntimeAssemblyRedirectInVS2015 =>
                    {
                        RedirectAssembly(needsRuntimeAssemblyRedirectInVS2015, new Version("14.0.0.0"), "b03f5f7f11d50a3a");
                    });
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
            _workspace = _componentModel.GetService<VisualStudioWorkspace>();

            if (_workspace == null)
            {
                throw new InvalidOperationException("workspace is null");
            }

            _workspace.WorkspaceChanged += _workspace_WorkspaceChanged;
        }

        private async void _workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                await DoOnSolutionReadyOrChange();
            }
        }

        private async Task DoOnSolutionReadyOrChange()
        {
            if (!File.Exists(_workspace.CurrentSolution.FilePath))
            {
                LogWarn("Could not find solution.");
                return;
            }

            if (!CurrentSolutionHasBitConfigV1JsonFile())
            {
                LogWarn("Could not find BitConfigV1.json file.");
                return;
            }

            _outputPane.Clear();

            DefaultBitConfigProvider configProvider = new DefaultBitConfigProvider();

            BitConfig config = null;

            try
            {
                config = configProvider.GetConfiguration(_workspace.CurrentSolution, Enumerable.Empty<Project>().ToList());

                foreach (BitCodeGeneratorMapping mapping in config.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
                {
                    if (!_workspace.CurrentSolution.Projects.Any(p => p.Name == mapping.DestinationProject.Name && p.Language == LanguageNames.CSharp))
                        throw new InvalidOperationException($"No project found named {mapping.DestinationProject.Name}");

                    foreach (BitTools.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (!_workspace.CurrentSolution.Projects.Any(p => p.Name == proj.Name && p.Language == LanguageNames.CSharp))
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
            return File.Exists(Path.Combine(Path.GetDirectoryName(_workspace.CurrentSolution.FilePath) + "\\BitConfigV1.json"));
        }

        private async Task GenerateCodes()
        {
            try
            {
                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()), dtosProvider
                    , new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(_workspace.CurrentSolution, _shouldGeneratedProjects);

                Log($"Code Generation Completed.");
            }
            catch (Exception ex)
            {
                LogException("Code Generation failed.", ex);
            }
        }

        private async Task CleanCodes()
        {
            DefaultHtmlClientProxyCleaner cleaner = new DefaultHtmlClientProxyCleaner(new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()));

            await cleaner.DeleteCodes(_workspace.CurrentSolution, _shouldGeneratedProjects);

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
                LogException($"Init html elements failed.", ex);
            }
        }

        private void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
        {
            Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
            {
                AssemblyName requestedAssembly = new AssemblyName(args.Name);

                if (requestedAssembly.Name != shortName)
                    return null;

                requestedAssembly.Version = targetVersion;
                requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
                requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

                AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;

                return Assembly.Load(requestedAssembly);
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
        }

        private async void _buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (!CurrentSolutionHasBitConfigV1JsonFile())
                return;

            if (generateCodesForTheFirstTimeExecuted == false || thereWasAnErrorInBuild == true)
            {
                _shouldGeneratedProjects = _workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList();
                await GenerateCodes();
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

            Project proj = _workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                                .ExtendedSingle($"Lookin for {project} in [ {(string.Join(",", _workspace.CurrentSolution.Projects.Select(prj => prj.Name)))} ]", prj => Path.GetFileName(prj.FilePath) == project.Split('\\').Last());

            _shouldGeneratedProjects.Add(proj);
        }

        private async void _buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (!CurrentSolutionHasBitConfigV1JsonFile())
                return;

            try
            {
                if (action == vsBuildAction.vsBuildActionClean)
                {
                    await CleanCodes();
                }
                else
                {
                    await GenerateCodes();
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
            if (_workspace != null)
                _workspace.WorkspaceChanged -= _workspace_WorkspaceChanged;

            if (_applicationObject?.Events?.BuildEvents != null)
            {
                _applicationObject.Events.BuildEvents.OnBuildProjConfigDone -= _buildEvents_OnBuildProjConfigDone;

                _applicationObject.Events.BuildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;

                _applicationObject.Events.BuildEvents.OnBuildDone -= _buildEvents_OnBuildDone;
            }

            base.Dispose(disposing);
        }

        private List<Project> _shouldGeneratedProjects;

        private VisualStudioWorkspace _workspace;

        private IComponentModel _componentModel;

        private IServiceContainer _serviceContainer;

        private OutputWindow _outputWindow;

        private OutputWindowPane _outputPane;

        private DTE2 _applicationObject;

        private IVsStatusbar _statusBar;

        private bool generateCodesForTheFirstTimeExecuted = false;

        private bool thereWasAnErrorInBuild = false;
    }
}
