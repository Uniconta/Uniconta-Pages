using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FinanceYearGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyFinanceYearClient); } }
        public override IComparer GridSorting { get { return new CompanyFinanceYearSort(); } }
        public override bool IsAutoSave { get { return false; } }
        public override bool CanDelete { get { return false; } }
    }

    public partial class AccountingYear : GridBasePage
    {
        readonly FinancialYearAPI FiApi;
        CompanyFinancePeriodClient[] AccountPeriodslist;

        public override string NameOfControl { get { return TabControls.AccountingYear.ToString(); } }
        protected override bool IsLayoutSaveRequired() { return false; }

        public AccountingYear(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgFinanceYearGrid);
            dgFinanceYearGrid.api = api;
            dgFinanceYearGrid.BusyIndicator = busyIndicatorFinanceYearGrid;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            FiApi = new FinancialYearAPI(api);
            ShowHideMenu();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.FinanceYearPage2)
                dgFinanceYearGrid.UpdateItemSource(argument);
        }       
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFinanceYearGrid.SelectedItem as CompanyFinanceYearClient;
            switch (ActionType)
            {
                case "AddRow":
                    var objectItemsForUpdate = new object[3];
                    objectItemsForUpdate[0] = api;
                    objectItemsForUpdate[1] = dgFinanceYearGrid;
                    objectItemsForUpdate[2] = "";
                    AddDockItem(TabControls.FinanceYearPage2, objectItemsForUpdate, Uniconta.ClientTools.Localization.lookup("AccountingYear"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    var objectItemsForUpdate2 = new object[2];
                    objectItemsForUpdate2[0] = selectedItem;
                    objectItemsForUpdate2[1] = dgFinanceYearGrid;
                    AddDockItem(TabControls.FinanceYearPage2, objectItemsForUpdate2, Uniconta.ClientTools.Localization.lookup("AccountingYear"), "Edit_16x16.png");
                    break;                          
                case "RecalcPeriodSum":
                    RecalcPeriodSum();
                    break;
                case "CheckPeriodSum":
                    CheckPeriodSum();
                    break;
                case "ResetFiscalYear":
                    ResetFiscalYear();
                    break;
                case "GeneratePrimo":
                    GeneratePrimo();
                    break;
                case "PrimoTransactions":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.AccountsTransaction, selectedItem, Uniconta.ClientTools.Localization.lookup("PrimoTransactions"));
                    break;
                case "AccountingPeriod":
                    genrateAccountingPeriod();
                    if (AccountPeriodslist != null)
                    {
                        CWAccountingPeriod ap = new CWAccountingPeriod(selectedItem, AccountPeriodslist, api);
                        ap.Show();
                    }
                    break;
                case "RemovePrimo":
                    if (selectedItem == null) return;
                    RemovePrimo(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void RemovePrimo(CompanyFinanceYearClient financialYearClient)
        {
            var removePrimoWindowDialog = new EraseYearWindow(financialYearClient.BalanceName, false);
            removePrimoWindowDialog.Closed += async delegate
            {
                if (removePrimoWindowDialog.DialogResult == true)
                {
                    var financialYearApi = new FinancialYearAPI(api);
                    var result = await financialYearApi.RemovePrimoTransactions(financialYearClient);
                    UtilDisplay.ShowErrorCode(result);
                }
            };
            removePrimoWindowDialog.Show();
        }
        CompanyFinanceYearClient getSelectedItem()
        {
            return dgFinanceYearGrid.SelectedItem as CompanyFinanceYearClient;
        }

        private void ResetFiscalYear()
        {
            var p = getSelectedItem();
            if (p == null)
                return;

            var interval = string.Format(Uniconta.ClientTools.Localization.lookup("FromTo"), p._FromDate.ToShortDateString(), p._ToDate.ToShortDateString());
            var Header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("DeleteTrans"), interval);

			var EraseYearWindowDialog = new EraseYearWindow(Header, false);
			EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    ErrorCodes res = await FiApi.EraseAllTransactions(p);
                    if (res != ErrorCodes.Succes)
                    { UtilDisplay.ShowErrorCode(res); }
                    else
                    {
                        dgFinanceYearGrid.RemoveFocusedRowFromGrid();
                        if (dgFinanceYearGrid.GetVisibleRows()?.Count > 0)
                            (dgFinanceYearGrid.View as TableView).FocusedRowHandle = 0;
                    }
                        
                }
            };
            EraseYearWindowDialog.Show();
        }

        private void GeneratePrimo()
        {
            var p = getSelectedItem();
            if (p == null)
                return;
            CWCreatePrimo dialogPrimo = new CWCreatePrimo(this.api,p);
            dialogPrimo.Closed += delegate
            {
                if (dialogPrimo.DialogResult == true)
                {
                    var s = string.Format(Uniconta.ClientTools.Localization.lookup("PrimoOnDate"), p._FromDate.ToShortDateString());
                    var EraseYearWindowDialog = new EraseYearWindow(s, true);
                    EraseYearWindowDialog.Closed += async delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                        {
                            ErrorCodes res = await FiApi.GeneratePrimoTransactions(p, dialogPrimo.BalanceName, dialogPrimo.PLText, dialogPrimo.Voucher, dialogPrimo.NumberserieText);
                            UtilDisplay.ShowErrorCode(res);
                            if (res == ErrorCodes.Succes && !p._Current)
                            {
                                var text = string.Format(Uniconta.ClientTools.Localization.lookup("MoveToOBJ"), string.Format("{0} - {1}", p._FromDate.ToShortDateString(), p._ToDate.ToShortDateString()));
                                CWConfirmationBox dialog = new CWConfirmationBox(text, Uniconta.ClientTools.Localization.lookup("IsYearEnded"), true);
                                dialog.Closing += delegate
                                {
                                    var res2 = dialog.ConfirmationResult;
                                    if (res2 == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                    {
                                        var source = (IList)dgFinanceYearGrid.ItemsSource;
                                        if (source != null)
                                        {
                                            IEnumerable<CompanyFinanceYearClient> gridItems = source.Cast<CompanyFinanceYearClient>();
                                            foreach (var y in gridItems)
                                            {
                                                if (y._Current)
                                                {
                                                    y.Current = false;
                                                    break;
                                                }
                                            }
                                        }
                                        var org = StreamingManager.Clone(p);
                                        p.Current = true;
                                        api.UpdateNoResponse(org, p);
                                    };
                                };
                                dialog.Show();
                            }
                        }
                    };
                    EraseYearWindowDialog.Show();
                }
            };
            dialogPrimo.Show();
        }

        async void CheckPeriodSum()
        {
            var p = getSelectedItem();
            if (p != null)
            {
                ErrorCodes res = await FiApi.CheckSumtables(p);
                UtilDisplay.ShowErrorCode(res);
            }
        }

        async void RecalcPeriodSum()
        {
            var p = getSelectedItem();
            if (p != null)
            {
                ErrorCodes res = await FiApi.RecalcSumtables(p);
                if (res == ErrorCodes.Succes)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CalculatingPeriods"), Uniconta.ClientTools.Localization.lookup("Message"));
                else
                    UtilDisplay.ShowErrorCode(res);
            }
        }

        void genrateAccountingPeriod()
        {
            var selectedItem = dgFinanceYearGrid.SelectedItem as CompanyFinanceYearClient;
            if (selectedItem == null)
                return;

            int l = selectedItem.NumberOfPeriods;
            AccountPeriodslist = new CompanyFinancePeriodClient[l];
            DateTime FromDate = selectedItem._FromDate;
            for (int i = 0; (i < l); i++)
            {
                CompanyFinancePeriodClient ax = new CompanyFinancePeriodClient();
                ax._Period = i;
                if (i == 0)
                    ax._FromDate = selectedItem.FromDate;
                else
                    ax.FromDate = AccountPeriodslist[i - 1].ToDate.AddDays(1);
                ax.ToDate = ax.FromDate.AddMonths(1).AddDays(-1);
                ax._State = selectedItem.PeriodStateGet(i);
                AccountPeriodslist[i] = ax;
            }
        }

        private void dgFinanceYearGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            if( (dgFinanceYearGrid.View as TableView).FocusedRowHandle  == dgFinanceYearGrid.VisibleRowCount - 1)
            {
                ribbonControl.EnableButtons( "ResetFiscalYear" );  
            }
            else
            {
                ribbonControl.DisableButtons("ResetFiscalYear" );
            }
        }

        void ShowHideMenu()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (api.session.User._Role <= (byte)Uniconta.Common.User.UserRoles.Reseller)
                UtilDisplay.RemoveMenuCommand(rb, "RecalcPeriodSum");
        }
    }
}
