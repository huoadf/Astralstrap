using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AstralstrapPage.xaml
    /// </summary>
    public partial class AstralstrapPage
    {
        public AstralstrapPage()
        {
            DataContext = new AstralstrapViewModel();
            InitializeComponent();
        }
    }
}