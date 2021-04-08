using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UnicontaClient.Utilities;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using Uniconta.Client.Pages;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWCalculateCommission.xaml
    /// </summary>
    public partial class CWCalculateCommission : ChildWindow
    {
        static DateTime _FromDateTime, _ToDateTime;
        public DateTime FromDateTime { get { return _FromDateTime; } set { _FromDateTime = value; } }
        public DateTime ToDateTime { get { return _ToDateTime; } set { _ToDateTime = value; } }

        public CWCalculateCommission(CrudAPI api)
        {
            if (_ToDateTime == DateTime.MinValue)
                _ToDateTime = BasePage.GetSystemDefaultDate();
            if (_FromDateTime == DateTime.MinValue)
                _FromDateTime = new DateTime(_ToDateTime.Year, _ToDateTime.Month, 01);

            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("CalculateCommission");

#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void CWCalculateCommission_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_OnClick(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
    }
}
