using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPurchaseAccountGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPurchaseAccountClient); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new InvPurchaseAccountSort(); } }

    }
    public partial class InvPurchaseAccountPage : GridBasePage
    {
        public override string NameOfControl{get { return TabControls.InvPurchaseAccountPage; } }

        public InvPurchaseAccountPage(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }
        public InvPurchaseAccountPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            localMenu.dataGrid = dgInvPurchaseAccount;
            SetRibbonControl(localMenu, dgInvPurchaseAccount);
            dgInvPurchaseAccount.api = api;
            dgInvPurchaseAccount.BusyIndicator = busyIndicator;
            dgInvPurchaseAccount.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvPurchaseAccount.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            if (master == null)
                Item.Visible = ItemName.Visible = true;
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvPurchaseAccountClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvPurchaseAccountGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvPurchaseAccountClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvPurchaseAccountGrid_PropertyChanged;
        }

        private void InvPurchaseAccountGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = (InvPurchaseAccountClient)sender;
            switch (e.PropertyName)
            {
                case "EAN":
                    if (!Utility.IsValidEAN(rec.EAN, api.CompanyEntity))
                    {
                        UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), rec.EAN), Uniconta.ClientTools.Localization.lookup("Warning"));
                        rec.EAN = null;
                    }
                    break;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPurchaseAccount.AddRow();
                    break;
                case "CopyRow":
                    dgInvPurchaseAccount.CopyRow();
                    break;
                case "SaveGrid":
                    dgInvPurchaseAccount.SaveData();
                    break;
                case "DeleteRow":
                    dgInvPurchaseAccount.DeleteRow();
                    break;
            }
        }
    }
}
