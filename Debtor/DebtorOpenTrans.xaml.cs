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

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Utilities;
using Uniconta.DataModel;
using System.Windows;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorOpenTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorTransOpenClient); } }
        public override bool Readonly { get { return false; } }
       
    }
    public partial class DebtorOpenTrans : GridBasePage
    {
        DebtorTransOpenClient tranOpenMaster;
        List<DebtorTransOpenClient> settles;
        UnicontaBaseEntity pageMaster;
        public override string NameOfControl
        {
            get { return TabControls.DebtorOpenTransactions.ToString(); }
        }
        public DebtorOpenTrans(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorTransOpen.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("OpenTrans"), BasePage.SetTableHeader(dgDebtorTransOpen.masterRecord));
            SetHeader(header);
        }
        public DebtorOpenTrans(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorTransOpen);
            dgDebtorTransOpen.api = api;
            dgDebtorTransOpen.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorTransOpen.UpdateMaster(pageMaster = master);
            dgDebtorTransOpen.tableView.ShowTotalSummary = true;

#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += CreditorOpenTrans_KeyDown;
#else
            this.KeyDown += CreditorOpenTrans_KeyDown;
#endif
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        private void CreditorOpenTrans_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                FlipSelection();
        }

        void FlipSelection()
        {
            var selRow = dgDebtorTransOpen.SelectedItem as DebtorTransOpenClient;
            if (selRow == null)
                return;
            selRow.IsSettled = !selRow.IsSettled;
            CheckBoxClick(selRow);
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorTranPage2)
                dgDebtorTransOpen.UpdateItemSource(argument);
        }

        async void Settle()
        {
            if (tranOpenMaster != null && settles != null)
            {
                TransactionAPI transApi = new TransactionAPI(api);
                var err = await transApi.Settle(tranOpenMaster, settles);
                tranOpenMaster = null;
                settles = null;
                UtilDisplay.ShowErrorCode(err);
                InitQuery();
            }
        }
       
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTransOpen.SelectedItem as DebtorTransOpenClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.DebtorTranPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("TransactionOutstanding"), ";component/Assets/img/Edit_16x16.png");
                    break;
                case "SettleTran":
                    Settle();
                    break;
                case "Settlements":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem.Voucher);
                    AddDockItem(TabControls.DebtorSettlements, selectedItem.Trans, header);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                    DebtorTransactions.ShowVoucher(dgDebtorTransOpen.syncEntity, api,busyIndicator);
                    break;
                case "SaveGrid":
                    dgDebtorTransOpen.SelectedItem = null;
                    dgDebtorTransOpen.SaveData();
                    break;
                case "ReopenAll":
                    ReOpenAllTrans();
                    break;
                case "AutoSettlement":
                    AutoSettlementTrans();
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgDebtorTransOpen.syncEntity, vheader);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void ReOpenAllTrans()
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {                  
                    TransactionAPI transApi = new TransactionAPI(this.api);
                    var masterAccount = dgDebtorTransOpen.masterRecord as DCAccount;
                    if (masterAccount != null)
                    {
                        busyIndicator.IsBusy = true;
                        var errorCodes = await transApi.ReOpenAll(masterAccount, true);
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
                    var masterAccount = dgDebtorTransOpen.masterRecord as DCAccount;
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
            var rec = (DebtorTransOpenClient)((CheckBox)sender).Tag;
            CheckBoxClick(rec);
        }

        void CheckBoxClick(DebtorTransOpenClient rec)
        {
            if (!rec.IsSettled)
            {
                if (tranOpenMaster == rec)
                {
                    tranOpenMaster = null;
                    settles = null;
                    var openTrans = dgDebtorTransOpen.ItemsSource as List<DebtorTransOpenClient>;
                    foreach (DebtorTransOpenClient openTran in openTrans)
                    {
                        openTran.IsSettled = false;
                    }
                }
                else if (settles != null && settles.Contains(rec))
                    settles.Remove(rec);
            }
            else
            {
                if (tranOpenMaster == null)
                    tranOpenMaster = rec;
                else
                {
                    if (settles == null)
                        settles = new List<DebtorTransOpenClient>();
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
            double? paymentCur =0d, invoiceCur=0d;
            if (tranOpenMaster != null)
            {
                payment = tranOpenMaster.AmountOpen;
                paymentCur = tranOpenMaster.AmountOpenCur.HasValue ? tranOpenMaster.AmountOpenCur.Value: 0d;
                if ((settles != null && settles.Count != 0))
                {
                    invoice = settles.Sum(s => s.AmountOpen);
                    invoiceCur= settles.Sum(s => s.AmountOpenCur);
                }
            }
            
            foreach (var grp in groups)
            {
                if (tranOpenMaster == null)
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
                        grp.StatusValue = paymentCur.HasValue ? paymentCur.Value.ToString("N2") : null;
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("InvoiceCurrency"))
                        grp.StatusValue = invoiceCur != 0d ? invoiceCur.Value.ToString("N2") : string.Empty;
                    else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("TotalCur"))
                        grp.StatusValue = invoiceCur != 0d ? (paymentCur + invoiceCur).Value.ToString("N2") : string.Empty;
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
            foreach (var row in dgDebtorTransOpen.ItemsSource as IEnumerable<DebtorTransOpenClient>)
            {
                if (row != pageMaster && row.IsSettled != value)
                {
                    row.IsSettled = value;
                    CheckBoxClick(row);
                }
            }
        }
    }
}
