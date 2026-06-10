using System.ComponentModel;

namespace FailFast
{
    internal sealed class FailFastOptions : BaseOptionModel<FailFastOptions>, IRatingConfig
    {

        [Browsable(false)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
