using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PaymentSetupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyPaymentSetupClient); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class PaymentSetup : GridBasePage
    {
        public PaymentSetup(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgPaymentSetupGrid.api = api;
            SetRibbonControl(localMenu, dgPaymentSetupGrid);
            dgPaymentSetupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            CompanyPaymentSetupClient selectedItem = dgPaymentSetupGrid.SelectedItem as CompanyPaymentSetupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgPaymentSetupGrid.AddRow();
                    break;
                case "CopyRow":
                    dgPaymentSetupGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgPaymentSetupGrid.DeleteRow();
                    break;
            }
        }

    }
}
