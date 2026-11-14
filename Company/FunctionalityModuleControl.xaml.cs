using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class FunctionalityModuleControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(FunctionalityModuleControl));

        public ImageSource IconSource{ get { return (ImageSource)GetValue(IconSourceProperty); } set { SetValue(IconSourceProperty, value); } }

        public FunctionalityModuleControl()
        {
            InitializeComponent();
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e) => (DataContext as FunctionalityModuleViewModel)?.SetExpanded(true);

        private void CollapseAll_Click(object sender, RoutedEventArgs e) => (DataContext as FunctionalityModuleViewModel)?.SetExpanded(false);
    }
}
