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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWBoolCheck.xaml
    /// </summary>
    public partial class CWPartnerInvoice : ChildWindow
    {
        public bool IsChecked { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get; set; }

        public CWPartnerInvoice()
        {
            InitializeComponent();
            DataContext = this;
            Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Email"));
            Height = 155.0d;
            rowHidden.Height = new GridLength(0.0d);
        }

        public CWPartnerInvoice(CrudAPI Api)
        {
            InitializeComponent();
            DataContext = this;
            Title = Uniconta.ClientTools.Localization.lookup("PostInvoice");
            Height = 200.0d;
            lookupJournal.api = Api;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }
    }
}
