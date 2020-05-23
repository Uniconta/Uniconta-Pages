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
using Uniconta.DataModel;

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
            ribbonControl.DisableButtons( "SaveGrid" );
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

        protected override void LoadCacheInBackGround()
        {
            LoadType(new List<Type>(2) { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.PaymentTerm) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTransOpen.SelectedItem as DebtorTransOpenClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgDebtorTransOpen.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorTranPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("TransactionOutstanding"), "Edit_16x16.png");
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
                case "SaveGrid":
                    saveGrid();
                    break;
                case "SendAsEmail":
                    if (selectedItem != null)
                        SendEmail(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SendEmail(DebtorTransOpenClient debtorTransOpen)
        {
            var postType = debtorTransOpen.Trans._PostType;
            DebtorEmailType emailType = DebtorEmailType.InterestNote;
            bool isInterest = false;
            if (postType != (byte)DCPostType.Collection || postType != (byte)DCPostType.CollectionLetter || postType != (byte)DCPostType.InterestFee || postType != (byte)DCPostType.PaymentCharge)
                return;

            if (postType == (byte)DCPostType.InterestFee)
                isInterest = true;
            if (postType == (byte)DCPostType.Collection)
                emailType = DebtorEmailType.Collection;
            else
            {
                CWCollectionLetter collectionLetterWin = new CWCollectionLetter();
                collectionLetterWin.Closed += delegate
                {
                    if (collectionLetterWin.DialogResult == true)
                        if (!Enum.TryParse(collectionLetterWin.Result, out emailType))
                            return;
                };
                collectionLetterWin.Show();
            }

            var cwSendInvoice = new CWSendInvoice();
#if !SILVERLIGHT
            cwSendInvoice.DialogTableId = 2000000031;
#endif
            cwSendInvoice.Closed += delegate
            {
                var selectedRow = new DebtorTransOpenClient[] { debtorTransOpen };
                var feelist = new [] { debtorTransOpen.Amount };

                if (cwSendInvoice.DialogResult == true)
                    DebtorPayments.ExecuteDebtorCollection(api, busyIndicator, selectedRow, feelist, null, false, emailType, cwSendInvoice.Emails,
                        cwSendInvoice.sendOnlyToThisEmail, isInterest);
            };
            cwSendInvoice.Show();
        }

        bool copyRowIsEnabled;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgDebtorTransOpen.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgDebtorTransOpen.MakeEditable();
                UserFieldControl.MakeEditable(dgDebtorTransOpen);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons( "SaveGrid" );
                copyRowIsEnabled = true;
                editAllChecked = false;
            }
            else
            {
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
                                var err = await dgDebtorTransOpen.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgDebtorTransOpen.Readonly = true;
                        dgDebtorTransOpen.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons( "SaveGrid" );
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorTransOpen.Readonly = true;
                    dgDebtorTransOpen.tableView.CloseEditor();
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
                return editAllChecked ? false : dgDebtorTransOpen.HasUnsavedData;
            }
        }
    }
}
