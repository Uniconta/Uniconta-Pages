using UnicontaClient;
using UnicontaClient.Models;
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
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CompanyPreferencePage : FormBasePage
    {
        CompanyClient editrow;

        public override Type TableType { get { return typeof(CompanyClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }
        protected override bool IsLayoutSaveRequired() { return false; }

        public override void OnClosePage(object[] refreshParams) { }

        public CompanyPreferencePage(UnicontaBaseEntity sourceData) : base(sourceData, true)
        {
            InitializeComponent();
            cmbOrdLineStorageReg.ItemsSource = AppEnums.StorageRegister.Values;
            cmbPurLineStorageReg.ItemsSource = AppEnums.StorageRegisterPurchage.Values;
            cmbAccOnInvTrans.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("DeliveryAccount"), Uniconta.ClientTools.Localization.lookup("InvoiceAccount") };
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            cmbAccOnInvTrans.SelectedIndex = (editrow.InvoiceAccountOnInvTrans) ? 1 : 0;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void cmbAccOnInvTrans_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            editrow.InvoiceAccountOnInvTrans = (cmbAccOnInvTrans.SelectedIndex != 0);
        }
    }
}
