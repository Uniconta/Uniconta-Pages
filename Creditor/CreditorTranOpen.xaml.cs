using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorTransOpenGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransOpenClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class CreditorTranOpen : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreditorTranOpen.ToString(); }
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties duedateSort = new SortingProperties("DueDate");
            duedateSort.Ascending = true;
            SortingProperties dateSort = new SortingProperties("Date");
            dateSort.Ascending = true;
            return new SortingProperties[] { duedateSort, dateSort };
        }

        public CreditorTranOpen(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorTranOpenGrid);
            dgCreditorTranOpenGrid.api = api;
            dgCreditorTranOpenGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var Comp = api.CompanyEntity;
            dgCreditorTranOpenGrid.tableView.ShowTotalSummary = true;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorTranOpenPage2)
                dgCreditorTranOpenGrid.UpdateItemSource(argument);
        }
        public CreditorTranOpen(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
        }

        private async void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorTranOpenGrid.SelectedItem as CreditorTransOpenClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CreditorTranOpenPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("AmountToPay"), ";component/Assets/img/Edit_16x16.png");
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                        DebtorTransactions.ShowVoucher(dgCreditorTranOpenGrid.syncEntity, api,busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null || selectedItem.Trans == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.Trans._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgCreditorTranOpenGrid.syncEntity, vheader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

    }
}
