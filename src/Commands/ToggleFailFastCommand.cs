namespace FailFast
{
    [Command(PackageIds.ToggleFailFastCommand)]
    internal sealed class ToggleFailFastCommand : BaseCommand<ToggleFailFastCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var options = await FailFastOptions.GetLiveInstanceAsync();
            options.Enabled = !options.Enabled;
            await options.SaveAsync();

            ((FailFastPackage)Package).ApplyEnabledState(options.Enabled);
            Command.Checked = options.Enabled;
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            base.BeforeQueryStatus(e);

            Command.Checked = FailFastOptions.Instance.Enabled;
        }
    }
}
