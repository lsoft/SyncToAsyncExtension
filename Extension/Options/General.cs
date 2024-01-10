using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Extension
{
    internal partial class OptionsProvider
    {
        // Register the options with this attribute on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Extension", "General", 0, 0, true, SupportsProfiles = true)]
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
    }
}
