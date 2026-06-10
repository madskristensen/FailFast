using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace FailFast
{
    internal sealed class BuildFailFastController : IDisposable
    {
        private readonly DTE2 _dte;
        private readonly IVsOutputWindowPane _buildOutputPane;
        private readonly IVsSolutionBuildManager2 _solutionBuildManager;
        private readonly EnvDTE.BuildEvents _buildEvents;
        private bool _canCancelBuild;

        private BuildFailFastController(DTE2 dte, IVsOutputWindowPane buildOutputPane, IVsSolutionBuildManager2 solutionBuildManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dte = dte;
            _buildOutputPane = buildOutputPane;
            _solutionBuildManager = solutionBuildManager;
            _buildEvents = _dte.Events.BuildEvents;

            _buildEvents.OnBuildBegin += OnBuildBegin;
            _buildEvents.OnBuildDone += OnBuildDone;
            _buildEvents.OnBuildProjConfigDone += OnProjectBuildFinished;
        }

        public bool Enabled { get; private set; } = true;

        public static async Task<BuildFailFastController> CreateAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get the "Build" Output Window pane
            IVsOutputWindow outputWindow = await package.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>() ?? throw new InvalidOperationException("Could not get SVsOutputWindow service.");

            Guid buildPaneGuid = VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref buildPaneGuid, out IVsOutputWindowPane? buildOutputPane));

            if (buildOutputPane == null)
            {
                throw new InvalidOperationException("Could not get build output pane.");
            }

            // Get additional services
            DTE2 dte = await package.GetServiceAsync<DTE, DTE2>() ?? throw new InvalidOperationException("Could not get DTE service.");
            IVsSolutionBuildManager2 buildManager = await package.GetServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>() ?? throw new InvalidOperationException("Could not get SVsSolutionBuildManager service.");

            return new BuildFailFastController(dte, buildOutputPane, buildManager);
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _buildEvents.OnBuildBegin -= OnBuildBegin;
            _buildEvents.OnBuildDone -= OnBuildDone;
            _buildEvents.OnBuildProjConfigDone -= OnProjectBuildFinished;
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            _canCancelBuild = true;
        }

        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            _canCancelBuild = false;
        }

        private void OnProjectBuildFinished(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_canCancelBuild || success || !Enabled)
            {
                return;
            }

            _canCancelBuild = false;
            CancelBuildImmediately();

            var projectName = Path.GetFileNameWithoutExtension(project);
            var message = $"FailFast: Build cancelled because project \"{projectName}\" failed at {DateTime.Now:HH:mm:ss}.{Environment.NewLine}";
            ErrorHandler.ThrowOnFailure(_buildOutputPane.OutputStringThreadSafe(message));
        }

        private void CancelBuildImmediately()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ErrorHandler.ThrowOnFailure(_solutionBuildManager.CanCancelUpdateSolutionConfiguration(out var canCancel));

            if (canCancel != 0)
            {
                ErrorHandler.ThrowOnFailure(_solutionBuildManager.CancelUpdateSolutionConfiguration());
            }
        }
    }
}
