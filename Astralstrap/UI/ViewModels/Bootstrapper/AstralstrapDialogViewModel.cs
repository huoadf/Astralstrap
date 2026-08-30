using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class AstralstrapDialogViewModel : BootstrapperDialogViewModel
    {
        public BackgroundType WindowBackdropType { get; set; } = BackgroundType.Mica;

        public SolidColorBrush BackgroundColourBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public AstralstrapDialogViewModel(IBootstrapperDialog dialog) : base(dialog)
        {
        }
    }
}