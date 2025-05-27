using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Grid;
using System;
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
using UnicontaClient.Utilities;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorOpenTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransOpenClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CreditorOpenTrans : GridBasePage
    {
        CreditorTransOpenClient transOpenMaster;
        List<CreditorTransOpenClient> settles;
        UnicontaBaseEntity pageMaster;
        public override string NameOfControl
        {
            get { return TabControls.CreditorOpenTransactions.ToString(); }
        }
        public CreditorOpenTrans(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorTranOpenGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("OpenTrans"), "/", BasePage.SetTableHeader(dgCreditorTranOpenGrid.masterRecord)));
        }
        public CreditorOpenTrans(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorTranOpenGrid);
            dgCreditorTranOpenGrid.api = api;
            dgCreditorTranOpenGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorTranOpenGrid.UpdateMaster(pageMaster = master);
            dgCreditorTranOpenGrid.ShowTotalSummary();
            this.KeyDown += CreditorOpenTrans_KeyDown;
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;

            HideMenuItems();
        }

        private void CreditorOpenTrans_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                FlipSelection();
        }

        void FlipSelection()
        {
            var selRow = dgCreditorTranOpenGrid.SelectedItem as CreditorTransOpenClient;
            if (selRow == null)
                return;
            selRow.IsSettled = !selRow.IsSettled;
            CheckBoxClick(selRow);
        }

        private void HideMenuItems()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
                UtilDisplay.RemoveMenuCommand(rb, "SendAsEmail");
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorTranOpenPage2)
            {
                api.ForcePrimarySQL = true;
                dgCreditorTranOpenGrid.UpdateItemSource(argument);
            }
        }
        async void Settle()
        {
            if (transOpenMaster != null && settles != null && settles.Count > 0)
            {
                TransactionAPI transApi = new TransactionAPI(api);
                var err = await transApi.Settle(transOpenMaster, settles);
                transOpenMaster = null;
                settles = null;
                UtilDisplay.ShowErrorCode(err);
                InitQuery();
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorTranOpenGrid.SelectedItem as CreditorTransOpenClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CreditorTranOpenPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("AmountToPay"), "Edit_16x16");
                    break;
                case "SettleTran":
                    Settle();
                    break;
                case "Settlements":
                    if (selectedItem == null)
                        return;
                    string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem.Voucher);
                    AddDockItem(TabControls.CreditorSettlements, selectedItem.Trans, header);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                    DebtorTransactions.ShowVoucher(dgCreditorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "ReopenAll":
                    ReOpenAllTrans(true);
                    break;
                case "ReopenCheck":
                    ReOpenAllTrans(false);
                    break;
                case "AutoSettlement":
                    AutoSettlementTrans();
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgCreditorTranOpenGrid.syncEntity, vheader);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void ReOpenAllTrans(bool LeaveAllOpen)
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    TransactionAPI transApi = new TransactionAPI(this.api);
                    var masterAccount = dgCreditorTranOpenGrid.masterRecord as DCAccount;
                    if (masterAccount != null)
                    {
                        busyIndicator.IsBusy = true;
                        var errorCodes = await transApi.ReOpenAll(masterAccount, true, LeaveAllOpen);
                        busyIndicator.IsBusy = false;
                        UtilDisplay.ShowErrorCode(errorCodes);
                        if (errorCodes == ErrorCodes.Succes)
                            InitQuery();
                    }
                }
            };
            dialog.Show();
        }

        private void AutoSettlementTrans()
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    TransactionAPI transApi = new TransactionAPI(this.api);
                    var masterAccount = dgCreditorTranOpenGrid.masterRecord as DCAccount;
                    if (masterAccount != null)
                    {
                        busyIndicator.IsBusy = true;
                        var errorCodes = await transApi.CloseAll(masterAccount);
                        busyIndicator.IsBusy = false;
                        UtilDisplay.ShowErrorCode(errorCodes);
                    }
                }
            };
            dialog.Show();
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var rec = (CreditorTransOpenClient)((System.Windows.Controls.CheckBox)sender).Tag;
            CheckBoxClick(rec);
        }
        private void CheckBoxClick(CreditorTransOpenClient rec)
        {
            if (!rec.IsSettled)
            {
                if (transOpenMaster == rec)
                {
                    transOpenMaster = null;
                    settles = null;
                    var openTrans = dgCreditorTranOpenGrid.ItemsSource as List<CreditorTransOpenClient>;
                    foreach (CreditorTransOpenClient openTran in openTrans)
                    {
                        openTran.IsSettled = false;
                    }
                }
                else if (settles != null && settles.Contains(rec))
                    settles.Remove(rec);
            }
            else
            {
                if (transOpenMaster == null)
                    transOpenMaster = rec;
                else
                {
                    if (settles == null)
                        settles = new List<CreditorTransOpenClient>();
                    settles.Add(rec);
                }
            }
            SetStatusText();
        }
        void SetStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            double payment = 0d, invoice = 0d;
            double paymentCur = 0d, invoiceCur = 0d;
            ShowPaymentStatus(true);
            if (transOpenMaster != null)
            {
                if (transOpenMaster.Currency == null)
                    payment = transOpenMaster.PartialSettlement != 0 ? transOpenMaster.PartialSettlement : transOpenMaster._AmountOpen;
                else
                {
                    payment = transOpenMaster.AmountOpen;
                    paymentCur = transOpenMaster.AmountOpenCur.GetValueOrDefault();
                }
                if ((settles != null && settles.Count != 0))
                {
                    foreach (var settle in settles)
                    {
                        if (settle.Currency == null)
                            invoice += settle.PartialSettlement != 0 ? settle.PartialSettlement : settle._AmountOpen;
                        else
                        {
                            invoiceCur += settle.PartialSettlement != 0 ? settle.PartialSettlement : settle._AmountOpenCur;
                            invoice += transOpenMaster._AmountOpen;
                        }
                    }
                    if (invoiceCur != 0d)
                        ShowPaymentCurrencyStatus(true);
                }
            }

            foreach (var grp in groups)
            {
                if (transOpenMaster == null)
                    grp.StatusValue = string.Empty;
                else
                {
                    if (grp.Caption == Uniconta.ClientTools.Localization.lookup("Payment"))
                        grp.StatusValue = payment.ToString("N2");
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("Invoice"))
                        grp.StatusValue = invoice != 0d ? invoice.ToString("N2") : string.Empty;
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("Sum"))
                        grp.StatusValue = invoice != 0d ? (payment + invoice).ToString("N2") : string.Empty;

                    if (grp.Caption == Uniconta.ClientTools.Localization.lookup("PaymentCurrency"))
                        grp.StatusValue = paymentCur != 0 ? paymentCur.ToString("N2") : null;
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("InvoiceCurrency"))
                        grp.StatusValue = invoiceCur != 0d ? invoiceCur.ToString("N2") : string.Empty;
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("TotalCur"))
                        grp.StatusValue = invoiceCur != 0d ? (paymentCur + invoiceCur).ToString("N2") : string.Empty;
                }
            }
        }

        private void SettleHeaderCheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            CheckSettle(true);
        }

        private void SettleHeaderCheckEditor_UnChecked(object sender, RoutedEventArgs e)
        {
            CheckSettle(false);
        }

        void CheckSettle(bool value)
        {
            if (pageMaster == null)
                return;
            foreach (var row in dgCreditorTranOpenGrid.ItemsSource as IEnumerable<CreditorTransOpenClient>)
            {
                if (row != pageMaster && row.IsSettled != value)
                {
                    row.IsSettled = value;
                    CheckBoxClick(row);
                }
            }
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(new List<Type>(2) { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.PaymentTerm) });
        }
        protected override void OnLayoutLoaded()
        {
            ShowPaymentStatus(false);
            ShowPaymentCurrencyStatus(false);
            base.OnLayoutLoaded();
        }

        void ShowPaymentStatus(bool val)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var items = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            SetStatusItemVisibility("Payment", items, val);
            SetStatusItemVisibility("Invoice", items, val);
            SetStatusItemVisibility("Sum", items, val, true);
            var gp = UtilDisplay.GetMenuGroupByItem(rb, Uniconta.ClientTools.Localization.lookup("Payment"));
            gp.IsVisible = val;
        }

        void ShowPaymentCurrencyStatus(bool val)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var items = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            SetStatusItemVisibility("PaymentCurrency", items, val);
            SetStatusItemVisibility("InvoiceCurrency", items, val);
            SetStatusItemVisibility("TotalCur", items, val, true);
            var gp = UtilDisplay.GetMenuGroupByItem(rb, Uniconta.ClientTools.Localization.lookup("PaymentCurrency"));
            gp.IsVisible = val;
        }
        private void SetStatusItemVisibility(string itemName, IEnumerable<ItemBase> items, bool isVisible, bool hideSeperator = false)
        {
            var item = items?.FirstOrDefault(i => Convert.ToString(i.Caption) == Uniconta.ClientTools.Localization.lookup(itemName));
            if (item != null)
                item.IsModule = isVisible;
        }
    }
}
