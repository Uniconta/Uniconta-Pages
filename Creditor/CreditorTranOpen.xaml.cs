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
using Uniconta.ClientTools.Util;
using Uniconta.Common.Utility;

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
            ribbonControl.DisableButtons( "SaveGrid");
            var Comp = api.CompanyEntity;
            dgCreditorTranOpenGrid.tableView.ShowTotalSummary = true;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorTranOpenPage2)
            {
                api.ForcePrimarySQL = true;
                dgCreditorTranOpenGrid.UpdateItemSource(argument);
            }
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

        protected override void LoadCacheInBackGround()
        {
            LoadType(new List<Type>(2) { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.PaymentTerm) } );
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorTranOpenGrid.SelectedItem as CreditorTransOpenClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgCreditorTranOpenGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CreditorTranOpenPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("AmountToPay"), "Edit_16x16");
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                        DebtorTransactions.ShowVoucher(dgCreditorTranOpenGrid.syncEntity, api,busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null || selectedItem.Trans == null)
                        return;
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.Trans._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgCreditorTranOpenGrid.syncEntity, vheader);
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem.Account);
                        AddDockItem(TabControls.CreditorInvoice, dgCreditorTranOpenGrid.syncEntity, header);
                    }
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        ShowInvoiceLines(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowInvoiceLines(CreditorTransOpenClient creditorTrans)
        {
            var creditorInvoice = await api.Query<CreditorInvoiceClient>(new UnicontaBaseEntity[] { creditorTrans }, null);
            if (creditorInvoice != null && creditorInvoice.Length > 0)
            {
                var credInv = creditorInvoice[0];
                AddDockItem(TabControls.CreditorInvoiceLine, credInv, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), credInv.InvoiceNum));
            }
        }

        bool copyRowIsEnabled;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCreditorTranOpenGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCreditorTranOpenGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCreditorTranOpenGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons( "SaveGrid" );
                copyRowIsEnabled = true;
                editAllChecked = false;
#if !SILVERLIGHT
                OnHold.ShowCheckBoxInHeader = Paid.ShowCheckBoxInHeader = true;
#endif
            }
            else
            {
#if !SILVERLIGHT
                OnHold.ShowCheckBoxInHeader = Paid.ShowCheckBoxInHeader = false;
#endif
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;
                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                var err = await dgCreditorTranOpenGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgCreditorTranOpenGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgCreditorTranOpenGrid.Readonly = true;
                        dgCreditorTranOpenGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons( "SaveGrid" );
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorTranOpenGrid.Readonly = true;
                    dgCreditorTranOpenGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons( "SaveGrid" );
                    copyRowIsEnabled = false;
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgCreditorTranOpenGrid.HasUnsavedData;
            }
        }

    }
}
