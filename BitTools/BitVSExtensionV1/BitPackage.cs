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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
                new[] { "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.Common", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.VisualBasic", "Microsoft.CodeAnalysis.VisualBasic.Workspaces", "Microsoft.CodeAnalysis.Workspaces", "Microsoft.VisualStudio.LanguageServices", "Microsoft.CodeAnalysis.Features" }.ToList()
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

            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));

            if (_componentModel == null)
            {
                ShowInitialLoadProblem("Component model is null");
                return;
            }

            Window outputWindow = _applicationObject.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (outputWindow == null)
            {
                ShowInitialLoadProblem("outputWindow is null");
                return;
            }

            _outputWindow = (OutputWindow)outputWindow.Object;

            if (!_outputWindow.OutputWindowPanes.Cast<OutputWindowPane>().Any(x => x.Name == BitVSExtensionName))
            {
                _outputWindow.OutputWindowPanes.Add(BitVSExtensionName);
            }

            _applicationObject.Events.BuildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;
            _applicationObject.Events.BuildEvents.OnBuildDone += _buildEvents_OnBuildDone;
            _applicationObject.Events.BuildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;

            if (await PrepareSolution() == false)
                return;

            await DoOnSolutionReadyOrChange();
        }

        private async Task<bool> PrepareSolution()
        {
            _workspace = _componentModel.GetService<VisualStudioWorkspace>();

            if (_workspace == null)
            {
                ShowInitialLoadProblem("Workspace is null");
                return false;
            }

            int tryCount = 0;

            while (!File.Exists(_workspace.CurrentSolution.FilePath))
            {
                if (tryCount == 60)
                    break;
                tryCount++;
                await Task.Delay(1000);
            }

            if (!File.Exists(_workspace.CurrentSolution.FilePath) || !File.Exists(Path.Combine(Path.GetDirectoryName(_workspace.CurrentSolution.FilePath) + "\\BitConfigV1.json")))
            {
                return false;
            }

            _isBeingBuiltProjects = new List<Project>();

            _workspace.WorkspaceChanged += _workspace_WorkspaceChanged;

            return true;
        }

        private async Task DoOnSolutionReadyOrChange()
        {
            try
            {
                InitHtmlElements();
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"Init html elements failed {ex}");
            }

            try
            {
                await GenerateAll();
            }
            catch (Exception ex)
            {
                ShowInitialLoadProblem($"Generate all failed {ex}");
            }
        }

        private async Task GenerateAll()
        {
            Stopwatch watch = Stopwatch.StartNew();

            IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
            IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

            DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()), dtosProvider
                , new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

            await generator.GenerateCodes(_workspace.CurrentSolution, _workspace.CurrentSolution.Projects.ToList());

            watch.Stop();

            Log($"Code Generation Completed in {watch.ElapsedMilliseconds} milli seconds");
        }

        private void InitHtmlElements()
        {
            HtmlElementsContainer.Elements = new List<BitHtmlElement> { };

            DefaultBitConfigProvider configProvider = new DefaultBitConfigProvider();

            BitConfig config = configProvider.GetConfiguration(_workspace.CurrentSolution, Enumerable.Empty<Project>().ToList());

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

        private async void _workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                await DoOnSolutionReadyOrChange();
            }
        }

        public void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
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

        private void _buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            _isBeingBuiltProjects.Clear();
        }

        private void _buildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            Project proj = _workspace.CurrentSolution.Projects
                .ExtendedSingle($"Lookin for {project} in [ {(string.Join(",", _workspace.CurrentSolution.Projects.Select(prj => prj.Name)))} ]", prj => prj.Language == "C#" && new FileInfo(prj.FilePath).Name == project.Split('\\').Last());

            if (proj != null)
            {
                _isBeingBuiltProjects.Add(proj);
            }
        }

        private void _buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            try
            {
                _buildEvents_OnBuildDone_Internal(scope, action);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw;
            }
        }

        private async void _buildEvents_OnBuildDone_Internal(vsBuildScope scope, vsBuildAction action)
        {
            if (action == vsBuildAction.vsBuildActionClean)
            {
                DefaultHtmlClientProxyCleaner cleaner = new DefaultHtmlClientProxyCleaner(new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()));

                await cleaner.DeleteCodes(_workspace.CurrentSolution, _isBeingBuiltProjects);

                Log("Generated codes were deleted");
            }
            else
            {
                Stopwatch watch = Stopwatch.StartNew();

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()), dtosProvider
                    , new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(_workspace.CurrentSolution, _isBeingBuiltProjects);

                watch.Stop();

                Log($"Code Generation Completed in {watch.ElapsedMilliseconds} milli seconds");
            }

        }

        public void Log(string text)
        {
            OutputWindowPane outputPane = _outputWindow.OutputWindowPanes.Cast<OutputWindowPane>()
                .SingleOrDefault(x => x.Name == BitVSExtensionName);

            if (outputPane == null)
            {
                MessageBox.Show(text, BitVSExtensionName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                outputPane.OutputString($"{text}\n");
            }
        }

        public void ShowInitialLoadProblem(string errorMessage)
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

        private List<Project> _isBeingBuiltProjects;

        private VisualStudioWorkspace _workspace;

        private IComponentModel _componentModel;

        private IServiceContainer _serviceContainer;

        private OutputWindow _outputWindow;

        private DTE2 _applicationObject;
    }
}
