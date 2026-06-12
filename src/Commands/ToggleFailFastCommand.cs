namespace FailFast.Commands
{
    [Command(PackageIds.ToggleFailFastCommand)]
    internal sealed class ToggleFailFastCommand : BaseCommand<ToggleFailFastCommand>
    {
        private static readonly Guid _buildOrDebugInactiveContext = new(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_string);
        private static readonly Guid _solutionHasMultipleProjectsContext = new(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionHasMultipleProjects_string);

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            FailFastOptions options = await FailFastOptions.GetLiveInstanceAsync();
            options.Enabled = !options.Enabled;
            await options.SaveAsync();

            Command.Checked = options.Enabled;
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Command.Visible = UIContext.FromUIContextGuid(_solutionHasMultipleProjectsContext).IsActive;
            Command.Checked = FailFastOptions.Instance.Enabled;
            Command.Enabled = UIContext.FromUIContextGuid(_buildOrDebugInactiveContext).IsActive;
        }
    }
}
