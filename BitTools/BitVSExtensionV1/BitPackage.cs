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
using System.Reflection;
using System.Globalization;

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

            _applicationObject = (DTE2)GetGlobalService(typeof(DTE));

            if (_applicationObject == null)
            {
                ShowInitialLoadProblem("applicationObject is null");
                return;
            }

            _applicationObject.Events.BuildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;
            _applicationObject.Events.BuildEvents.OnBuildDone += _buildEvents_OnBuildDone;
            _applicationObject.Events.BuildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;

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

            string version = _applicationObject.Version;

            if (version == "14.0" /*VS 2015*/)
            {
                new[] { "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.Common", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.VisualBasic", "Microsoft.CodeAnalysis.VisualBasic.Workspaces", "Microsoft.CodeAnalysis.Workspaces", "Microsoft.VisualStudio.LanguageServices", }.ToList()
                    .ForEach(needsRuntimeAssemblyRedirectInVS2015 =>
                    {
                        RedirectAssembly(needsRuntimeAssemblyRedirectInVS2015, new Version("1.3.1.0"), "31bf3856ad364e35");
                    });

                RedirectAssembly("System.Collections.Immutable", new Version("1.1.37"), "b03f5f7f11d50a3a");
            }
        }

        public void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
        {
            ResolveEventHandler handler = null;

            handler = (sender, args) =>
            {
                AssemblyName requestedAssembly = new AssemblyName(args.Name);

                if (requestedAssembly.Name != shortName)
                    return null;

                requestedAssembly.Version = targetVersion;
                requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
                requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

                AppDomain.CurrentDomain.AssemblyResolve -= handler;

                return Assembly.Load(requestedAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;
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
                _buildEvents_OnBuildDone_Internal(scope, action);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw;
            }
        }

        private void _buildEvents_OnBuildDone_Internal(vsBuildScope scope, vsBuildAction action)
        {
            Solution solution = _workspace.CurrentSolution;

            if (action == vsBuildAction.vsBuildActionClean)
            {
                DefaultHtmlClientProxyCleaner cleaner = new DefaultHtmlClientProxyCleaner(new DefaultBitCodeGeneratorMappingsProvider(new DefaultBitConfigProvider()));
                cleaner.GetType().GetTypeInfo().GetMethod(nameof(DefaultHtmlClientProxyCleaner.DeleteCodes))
                    .Invoke(cleaner, new object[] { _workspace, solution, _isBeingBuiltProjects });

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

                generator.GetType().GetTypeInfo().GetMethod(nameof(DefaultHtmlClientProxyGenerator.GenerateCodes))
                    .Invoke(generator, new object[] { _workspace, solution, _isBeingBuiltProjects });

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
            if (_applicationObject?.Events?.BuildEvents != null)
            {
                _applicationObject.Events.BuildEvents.OnBuildProjConfigDone -= _buildEvents_OnBuildProjConfigDone;

                _applicationObject.Events.BuildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;

                _applicationObject.Events.BuildEvents.OnBuildDone -= _buildEvents_OnBuildDone;
            }

            base.Dispose(disposing);
        }

        private readonly List<Project> _isBeingBuiltProjects = new List<Project>();

        private VisualStudioWorkspace _workspace;

        private IServiceContainer _serviceContainer;

        private OutputWindow _outputWindow;

        private DTE2 _applicationObject;
    }
}
