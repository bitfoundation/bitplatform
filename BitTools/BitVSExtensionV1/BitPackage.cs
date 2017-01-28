using System.Collections.Generic;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;
using System.Windows.Forms;
using System;
using System.IO;
using EnvDTE80;
using System.Diagnostics;
using BitTools.Core.Contracts;
using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.HtmlClientProxyGenerator;

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

        public BitPacakge()
        {

        }

        protected override async void Initialize()
        {
            base.Initialize();

            IComponentModel componentModel = (IComponentModel)GetService(typeof(SComponentModel));

            if (componentModel == null)
            {
                ShowInitialLoadProblem("Component model is null");
                return;
            }

            _workspace = componentModel.GetService<VisualStudioWorkspace>();

            if (_workspace == null)
            {
                ShowInitialLoadProblem("Workspace is null");
                return;
            }

            int tryCount = 0;

            while (!File.Exists(_workspace.CurrentSolution.FilePath))
            {
                if (tryCount == 60)
                    break;
                tryCount++;
                await System.Threading.Tasks.Task.Delay(1000);
            }

            if (!File.Exists(_workspace.CurrentSolution.FilePath))
            {
                return;
            }

            _serviceContainer = this;

            _dte = (DTE)_serviceContainer.GetService(typeof(SDTE));

            if (_dte == null)
            {
                ShowInitialLoadProblem("dte is null");
                return;
            }

            _buildEvents = _dte.Events.BuildEvents;

            if (_buildEvents == null)
            {
                ShowInitialLoadProblem("buildEvents is null");
                return;
            }

            _buildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;

            _buildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;

            _buildEvents.OnBuildDone += _buildEvents_OnBuildDone;

            _applicationObject = Processes.GetDTE();

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

            _outputWindow = (OutputWindow)outputWindow.Object;

            if (!_outputWindow.OutputWindowPanes.Cast<OutputWindowPane>().Any(x => x.Name == BitVSExtensionName))
            {
                _outputWindow.OutputWindowPanes.Add(BitVSExtensionName);
            }
        }

        private void _buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            _isBeingBuiltProjects.Clear();
        }

        private void _buildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            Project proj = _workspace.CurrentSolution.Projects
                .Single(prj => prj.Language == "C#" && new FileInfo(prj.FilePath).Name == project.Split('\\').Last());

            if (proj != null)
            {
                _isBeingBuiltProjects.Add(proj);
            }
        }

        private void _buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            try
            {
                Solution solution = _workspace.CurrentSolution;

                if (action == vsBuildAction.vsBuildActionClean)
                {
                    new DefaultHtmlClientProxyCleaner(new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()))
                            .DeleteCodes(_workspace, solution, _isBeingBuiltProjects);

                    Log("Generated codes were deleted");
                }
                else
                {
                    Stopwatch watch = Stopwatch.StartNew();

                    IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                    IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                    new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                        new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()), dtosProvider
                        , new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider))
                            .GenerateCodes(_workspace, solution, _isBeingBuiltProjects);

                    watch.Stop();

                    Log($"Code Generation Completed in {watch.ElapsedMilliseconds} milli seconds");
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw;
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
            _buildEvents.OnBuildProjConfigDone -= _buildEvents_OnBuildProjConfigDone;

            _buildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;

            _buildEvents.OnBuildDone -= _buildEvents_OnBuildDone;

            base.Dispose(disposing);
        }

        private readonly List<Project> _isBeingBuiltProjects = new List<Project>();

        private BuildEvents _buildEvents;

        private DTE _dte;

        private VisualStudioWorkspace _workspace;

        private IServiceContainer _serviceContainer;

        private OutputWindow _outputWindow;

        private DTE2 _applicationObject;
    }
}
