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
    public class DebtorTransOpenGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(DebtorTransOpenClient); } }
    }
    public partial class DebtorTranOpen : GridBasePage
    {      
        public override string NameOfControl
        {
            get { return TabControls.DebtorTranOpen.ToString(); }
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties duedateSort = new SortingProperties("DueDate");
            SortingProperties dateSort = new SortingProperties("Date");
            return new SortingProperties[] { duedateSort, dateSort };
        }

        public DebtorTranOpen(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorTransOpen);
            dgDebtorTransOpen.api = api;
            dgDebtorTransOpen.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var Comp = api.CompanyEntity;
            dgDebtorTransOpen.tableView.ShowTotalSummary = true;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorTranPage2)
                dgDebtorTransOpen.UpdateItemSource(argument);
        }
        public DebtorTranOpen(BaseAPI api, string lookupKey)
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

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTransOpen.SelectedItem as DebtorTransOpenClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorTranPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("TransactionOutstanding"), ";component/Assets/img/Edit_16x16.png");
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgDebtorTransOpen.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem?.Trans == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.Trans._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgDebtorTransOpen.syncEntity, vheader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
