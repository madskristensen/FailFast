global using Community.VisualStudio.Toolkit;

global using Microsoft.VisualStudio.Shell;

global using System;

global using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio;

using System.Runtime.InteropServices;
using System.Threading;

namespace FailFast
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.FailFastString)]
    public sealed class FailFastPackage : ToolkitPackage
    {
        private BuildFailFastController? _controller;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();

            var options = await FailFastOptions.GetLiveInstanceAsync();
            _controller = await BuildFailFastController.CreateAsync(this);
            _controller.SetEnabled(options.Enabled);
        }

        internal void ApplyEnabledState(bool enabled) => _controller?.SetEnabled(enabled);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
