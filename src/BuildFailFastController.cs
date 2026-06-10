using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace FailFast
{
    internal sealed class BuildFailFastController : IDisposable
    {
        private readonly IVsOutputWindowPane _buildOutputPane;
        private readonly IVsSolutionBuildManager2 _solutionBuildManager;
        private bool _canCancelBuild;

        private BuildFailFastController(IVsOutputWindowPane buildOutputPane, IVsSolutionBuildManager2 solutionBuildManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _buildOutputPane = buildOutputPane;
            _solutionBuildManager = solutionBuildManager;

            // ProjectBuildDone only fires for build/rebuild operations. Clean operations raise the
            // separate ProjectCleanDone event, so a project that fails to clean (e.g. a failed COM
            // unregistration step) never triggers a fail-fast cancellation of the whole solution.
            VS.Events.BuildEvents.SolutionBuildStarted += OnSolutionBuildStarted;
            VS.Events.BuildEvents.SolutionBuildDone += OnSolutionBuildDone;
            VS.Events.BuildEvents.SolutionBuildCancelled += OnSolutionBuildCancelled;
            VS.Events.BuildEvents.ProjectBuildDone += OnProjectBuildDone;
        }

        public bool Enabled { get; private set; } = true;

        public static async Task<BuildFailFastController> CreateAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsOutputWindow outputWindow = await package.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>() ?? throw new InvalidOperationException("Could not get SVsOutputWindow service.");

            Guid buildPaneGuid = VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref buildPaneGuid, out IVsOutputWindowPane? buildOutputPane));

            if (buildOutputPane == null)
            {
                throw new InvalidOperationException("Could not get build output pane.");
            }

            IVsSolutionBuildManager2 buildManager = await package.GetServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>() ?? throw new InvalidOperationException("Could not get SVsSolutionBuildManager service.");

            return new BuildFailFastController(buildOutputPane, buildManager);
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public void Dispose()
        {
            VS.Events.BuildEvents.SolutionBuildStarted -= OnSolutionBuildStarted;
            VS.Events.BuildEvents.SolutionBuildDone -= OnSolutionBuildDone;
            VS.Events.BuildEvents.SolutionBuildCancelled -= OnSolutionBuildCancelled;
            VS.Events.BuildEvents.ProjectBuildDone -= OnProjectBuildDone;
        }

        private void OnSolutionBuildStarted(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _canCancelBuild = true;
        }

        private void OnSolutionBuildDone(bool succeeded)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _canCancelBuild = false;
        }

        private void OnSolutionBuildCancelled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _canCancelBuild = false;
        }

        private void OnProjectBuildDone(ProjectBuildDoneEventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_canCancelBuild || !Enabled || args.IsSuccessful)
            {
                return;
            }

            _canCancelBuild = false;
            CancelBuildImmediately();

            var projectName = args.Project?.Name ?? "Unknown";
            var message = $"{Vsix.Name}: Build cancelled because project \"{projectName}\" failed at {DateTime.Now:HH:mm:ss}.{Environment.NewLine}";
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
