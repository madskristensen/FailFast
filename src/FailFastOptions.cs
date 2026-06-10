using System.ComponentModel;

namespace FailFast
{
    internal sealed class FailFastOptions : BaseOptionModel<FailFastOptions>
    {
        [Browsable(false)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;
    }
}
