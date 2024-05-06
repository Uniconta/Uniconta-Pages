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

        public override string NameOfControl { get { return TabControls.AccountingYear; } }
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
            {
                dgFinanceYearGrid.UpdateItemSource(argument);
                object[] argumentParams = argument as object[];
                if (argumentParams != null && argumentParams.Length >= 2 && (int)argumentParams[0] == 1) // insert
                {
                    var Row = argumentParams[1] as CompanyFinanceYearClient;
                    if (Row != null && Math.Abs((Row._FromDate - BasePage.GetSystemDefaultDate()).TotalDays) < 200)
                    {
                        if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CreateOpeningBalance"),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            GeneratePrimo(Row);
                        }
                    }
                }
            }
        }
        
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFinanceYearGrid.SelectedItem as CompanyFinanceYearClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.FinanceYearPage2, new object[] { api, dgFinanceYearGrid, "" }, Uniconta.ClientTools.Localization.lookup("AccountingYear"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.FinanceYearPage2, new object[] { selectedItem, dgFinanceYearGrid }, Uniconta.ClientTools.Localization.lookup("AccountingYear"), "Edit_16x16");
                    break;                          
                case "RecalcPeriodSum":
                    if (selectedItem != null)
                        RecalcPeriodSum(selectedItem);
                    break;
                case "CheckPeriodSum":
                    if (selectedItem != null)
                        CheckPeriodSum(selectedItem);
                    break;
                case "EraseFiscalYear":
                    if (selectedItem != null)
                        EraseFiscalYear(selectedItem);
                    break;
                case "GeneratePrimo":
                    if (selectedItem != null)
                        GeneratePrimo(selectedItem);
                    break;
                case "PrimoTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, selectedItem, Uniconta.ClientTools.Localization.lookup("PrimoTransactions"));
                    break;
                case "AccountingPeriod":
                    if (selectedItem != null)
                    {
                        genrateAccountingPeriod(selectedItem);
                        if (AccountPeriodslist != null)
                        {
                            new CWAccountingPeriod(selectedItem, AccountPeriodslist, api).Show();
                        }
                    }
                    break;
                case "RemovePrimo":
                    if (selectedItem != null)
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

        private void EraseFiscalYear(CompanyFinanceYearClient p)
        {
            var interval = string.Format(Uniconta.ClientTools.Localization.lookup("FromTo"), p._FromDate.ToShortDateString(), p._ToDate.ToShortDateString());
            var Header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("DeleteTrans"), interval);

			var EraseYearWindowDialog = new EraseYearWindow(Header, false);
			EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    ErrorCodes res = await FiApi.EraseAllTransactions(p);
                    UtilDisplay.ShowErrorCode(res);
                    if (res == ErrorCodes.Succes)
                    {
                        dgFinanceYearGrid.RemoveFocusedRowFromGrid();
                        if (dgFinanceYearGrid.GetVisibleRows()?.Count > 0)
                            (dgFinanceYearGrid.View as TableView).FocusedRowHandle = 0;
                    }
                }
            };
            EraseYearWindowDialog.Show();
        }

        private void GeneratePrimo(CompanyFinanceYearClient p)
        {
            CWCreatePrimo dialogPrimo = new CWCreatePrimo(this.api,p);
            dialogPrimo.Closed += delegate
            {
                if (dialogPrimo.DialogResult == true)
                {
                    var s = string.Format(Uniconta.ClientTools.Localization.lookup("PrimoOnDate"), p._FromDate.ToShortDateString());
                    var EraseYearWindowDialog = new EraseYearWindow(s, true);
                    EraseYearWindowDialog.Closed += delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                            runPrimo(dialogPrimo, p);
                    };
                    EraseYearWindowDialog.Show();
                }
            };
            dialogPrimo.Show();
        }

        async void runPrimo(CWCreatePrimo dialogPrimo, CompanyFinanceYearClient p)
        {
            ErrorCodes res = await FiApi.GeneratePrimoTransactions(p, dialogPrimo.BalanceName, dialogPrimo.PLText, dialogPrimo.Voucher, dialogPrimo.NumberserieText);
            if (res == ErrorCodes.Succes && !p._Current && p._FromDate <= DateTime.Now && p._ToDate >= DateTime.Now)
            {
                var text = string.Format(Uniconta.ClientTools.Localization.lookup("MoveToOBJ"), string.Format("{0} - {1}", p._FromDate.ToShortDateString(), p._ToDate.ToShortDateString()));
                CWConfirmationBox dialog = new CWConfirmationBox(text, Uniconta.ClientTools.Localization.lookup("IsYearEnded"), true);
                dialog.Closing += delegate
                {
                    if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    {
                        var source = (IList)dgFinanceYearGrid.ItemsSource;
                        if (source != null)
                        {
                            foreach (var y in source.Cast<CompanyFinanceYearClient>())
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
            else if (res == ErrorCodes.AccountIsMissing)
            {
                var cw = new CwSystemAccountType(api, "AccountTypeYearResultTransfer");
                cw.Closing += async delegate
                {
                    var glAcc = cw.GlAccount;
                    if (cw.DialogResult == true && glAcc != null && glAcc.AccountTypeEnum >= Uniconta.DataModel.GLAccountTypes.PL)
                    {
                        var loaded = StreamingManager.Clone(glAcc);
                        glAcc._SystemAccount = (byte)Uniconta.DataModel.SystemAccountTypes.EndYearResultTransfer;
                        var err = await api.Update(loaded, glAcc);
                        if (err != 0)
                            UtilDisplay.ShowErrorCode(err);
                        else
                            runPrimo(dialogPrimo, p);
                    }
                };
                cw.Show();
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void CheckPeriodSum(CompanyFinanceYearClient p)
        {
            UtilDisplay.ShowErrorCode(await FiApi.CheckSumtables(p));
        }

        async void RecalcPeriodSum(CompanyFinanceYearClient p)
        {
            ErrorCodes res = await FiApi.RecalcSumtables(p);
            if (res == ErrorCodes.Succes)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CalculatingPeriods"), Uniconta.ClientTools.Localization.lookup("Message"));
            else
                UtilDisplay.ShowErrorCode(res);
        }

        void genrateAccountingPeriod(CompanyFinanceYearClient selectedItem)
        {
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
            if( (dgFinanceYearGrid.View as TableView).FocusedRowHandle == dgFinanceYearGrid.VisibleRowCount - 1)
            {
                ribbonControl.EnableButtons("EraseFiscalYear");
                ribbonControl.DisableButtons(new[] { "GeneratePrimo", "RemovePrimo" });
            }
            else
            {
                ribbonControl.DisableButtons("EraseFiscalYear");
                ribbonControl.EnableButtons(new[] { "GeneratePrimo", "RemovePrimo" });
            }
        }

        void ShowHideMenu()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.SystemAdmin)
                UtilDisplay.RemoveMenuCommand(rb, "RecalcPeriodSum");
        }
    }
}
