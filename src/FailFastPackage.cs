global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;

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

            FailFastOptions options = await FailFastOptions.GetLiveInstanceAsync();
            _controller = await BuildFailFastController.CreateAsync(this, options);
        }

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
