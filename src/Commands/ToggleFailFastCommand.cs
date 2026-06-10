namespace FailFast.Commands
{
    [Command(PackageIds.ToggleFailFastCommand)]
    internal sealed class ToggleFailFastCommand : BaseCommand<ToggleFailFastCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            FailFastOptions options = await FailFastOptions.GetLiveInstanceAsync();
            options.Enabled = !options.Enabled;
            await options.SaveAsync();

            ((FailFastPackage)Package).ApplyEnabledState(options.Enabled);
            Command.Checked = options.Enabled;
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Checked = FailFastOptions.Instance.Enabled;
        }
    }
}
