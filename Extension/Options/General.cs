using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Extension
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General>
        {
        }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("General")]
        [DisplayName("Enabled")]
        [Description("Specifies whether to activate the CodeLens for sync<->async moving or not.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Category("General")]
        [DisplayName("Recalculate timeout")]
        [Description("Specifies how many seconds after last typing this extension waits to recalculate codelenses.")]
        [DefaultValue(true)]
        public int TimeoutToRecalculate { get; set; } = 3;
    }
}
