using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace FailFast
{
    internal sealed class BuildFailFastController : IDisposable, IVsUpdateSolutionEvents2
    {
        private readonly IVsOutputWindowPane _buildOutputPane;
        private readonly IVsSolutionBuildManager2 _solutionBuildManager;
        private readonly FailFastOptions _options;
        private readonly RatingPrompt _prompt;
        private readonly List<IVsHierarchy> _failedProjects = [];
        private readonly uint _buildEventsCookie;
        private bool _usageRegistered;

        private BuildFailFastController(IVsOutputWindowPane buildOutputPane, IVsSolutionBuildManager2 solutionBuildManager, FailFastOptions options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _buildOutputPane = buildOutputPane;
            _solutionBuildManager = solutionBuildManager;
            _options = options;
            _prompt = new RatingPrompt("MadsKristensen.FailFast", Vsix.Name, options);

            ErrorHandler.ThrowOnFailure(_solutionBuildManager.AdviseUpdateSolutionEvents(this, out _buildEventsCookie));
        }

        public static async Task<BuildFailFastController> CreateAsync(AsyncPackage package, FailFastOptions options)
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

            return new BuildFailFastController(buildOutputPane, buildManager, options);
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(_solutionBuildManager.UnadviseUpdateSolutionEvents(_buildEventsCookie));
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => OnSolutionBuildStarted();

        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate) => OnSolutionBuildStarted();

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => OnSolutionBuildFinished();

        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => OnSolutionBuildFinished();

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => OnSolutionBuildFinished();

        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel() => OnSolutionBuildFinished();

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_options.Enabled || pfCancel != 0 || !IsBuild(dwAction))
            {
                return VSConstants.S_OK;
            }

            foreach (IVsHierarchy failedProject in _failedProjects)
            {
                ErrorHandler.ThrowOnFailure(_solutionBuildManager.QueryProjectDependency(pHierProj, failedProject, out var isDependent));

                if (isDependent != 0)
                {
                    pfCancel = 1;
                    break;
                }
            }

            if (pfCancel != 0)
            {
                WriteMessage($"Skipped project \"{GetProjectName(pHierProj)}\" because it depends on a failed project.");

                if (!_usageRegistered)
                {
                    _prompt.RegisterSuccessfulUsage();
                    _usageRegistered = true;
                }
            }

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_options.Enabled && IsBuild(dwAction) && fSuccess == 0 && fCancel == 0)
            {
                _failedProjects.Add(pHierProj);
                WriteMessage($"Project \"{GetProjectName(pHierProj)}\" failed. Dependent projects will be skipped.");
            }

            return VSConstants.S_OK;
        }

        private int OnSolutionBuildStarted()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _failedProjects.Clear();
            _usageRegistered = false;

            if (_options.Enabled)
            {
                ErrorHandler.ThrowOnFailure(_solutionBuildManager.CalculateProjectDependencies());
            }

            return VSConstants.S_OK;
        }

        private int OnSolutionBuildFinished()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _failedProjects.Clear();
            return VSConstants.S_OK;
        }

        private void WriteMessage(string message)
        {
            ErrorHandler.ThrowOnFailure(_buildOutputPane.OutputStringThreadSafe($"{Vsix.Name}: {message} {DateTime.Now:HH:mm:ss}.{Environment.NewLine}"));
        }

        private static bool IsBuild(uint action)
        {
            return ((VSSOLNBUILDUPDATEFLAGS)action & VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD) != 0;
        }

        private static string GetProjectName(IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out object name));
            return name as string ?? "Unknown";
        }
    }
}
