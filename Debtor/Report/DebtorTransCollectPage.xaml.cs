using System;
using System.Collections;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorTransCollectGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorTransCollectClient); } }

        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting => new DCTransCollectSort();
    }

    public partial class DebtorTransCollectPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorTransCollectPage; } }

        SQLCache accountCache;
        public DebtorTransCollectPage(UnicontaBaseEntity masterRecord) : base(masterRecord)
        {
            InitializeComponent();
            dgDebtorTransCollect.UpdateMaster(masterRecord);
            InitPage();
        }

        public DebtorTransCollectPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitializeComponent();
            dgDebtorTransCollect.UpdateMaster(syncEntity.Row);
            InitPage();
        }
        public DebtorTransCollectPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            dgDebtorTransCollect.api = api;
            SetRibbonControl(localMenu, dgDebtorTransCollect);
            dgDebtorTransCollect.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorTransCollect.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgDebtorTransCollect.masterRecord;
            if (syncMaster == null)
                return;
            var invoiceANProp = syncMaster.GetType().GetProperty("InvoiceAN");//for DebtorTrans and DebtorTransOpen
            if (invoiceANProp == null)
                invoiceANProp = syncMaster.GetType().GetProperty("Account");//for DebtorAccount
            var invoiceAN = Convert.ToString(invoiceANProp.GetValue(syncMaster, null));
            if (!string.IsNullOrEmpty(invoiceAN))
            {
                string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CollectionLetterLog"), invoiceAN);
                SetHeader(header);
            }
        }
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            accountCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTransCollect.SelectedItem as DebtorTransCollectClient;
            switch (ActionType)
            {
                case "SendAsEmail":
                    if (selectedItem?.OpenTran != null)
                    {
                        var cwSendInvoice = new CWSendInvoice();
                        cwSendInvoice.DialogTableId = 2000000031;
                        cwSendInvoice.Closed += delegate
                        {
                            selectedItem.OpenTran._Code = selectedItem._Code;
                            DebtorPayments.ExecuteDebtorCollection(api, busyIndicator, new[] { (DebtorTransOpen)selectedItem.OpenTran }, false, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail, (selectedItem.OpenTran.Trans._PostType == (byte)DCPostType.InterestFee));
                        };
                        cwSendInvoice.Show();
                    }
                    break;
                case "SendAsOutlook":
                    if (selectedItem?.OpenTran != null)
                        OpenOutlook(selectedItem);
                    break;
                case "Print":
                    if (selectedItem?.OpenTran != null)
                        PrintData(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void OpenOutlook(DebtorTransCollectClient selectedItem)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var debtor = accountCache.Get(selectedItem.Trans._Account) as DebtorClient;

                selectedItem.OpenTran._Code = selectedItem._Code;
                var paymentStandardReport = await GeneratePrintReport(selectedItem.OpenTran, debtor, selectedItem._Date, selectedItem._Code);
                if (paymentStandardReport != null)
                {
                    InvoicePostingPrintGenerator.OpenReportInOutlook(api, paymentStandardReport, debtor, selectedItem._Code);
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        async private void PrintData(DebtorTransCollectClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                byte[] logo = await UtilCommon.GetLogo(api);
                var Comp = api.CompanyEntity;

                var companyClient = Comp.CreateUserType<CompanyClient>();
                StreamingManager.Copy(Comp, companyClient);

                selectedItem.OpenTran._Code = selectedItem._Code;
                var debtor = accountCache.Get(selectedItem.Trans._Account) as DebtorClient;

                var paymentStandardReport = await GeneratePrintReport(selectedItem.OpenTran, debtor, selectedItem._Date, selectedItem._Code);
                var reportName = selectedItem._Code == DebtorEmailType.InterestNote ? Uniconta.ClientTools.Localization.lookup("InterestNote") : Uniconta.ClientTools.Localization.lookup("CollectionLetter");
                var dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), reportName);
                AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { paymentStandardReport, reportName }, dockName);
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("DebtorPayment.PrintData(), CompanyId={0}", api.CompanyId));
                UnicontaMessageBox.Show(ex);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        async private Task<IPrintReport> GeneratePrintReport(DCTransOpen payment, DebtorClient debtor, DateTime date, DebtorEmailType debtorEmailType)
        {
            var deb = new DebtorPaymentStatementList()
            {
                AccountNumber = debtor._Account,
                Name = debtor._Name,
            };
            var rec = new DebtorTransPayment();
            StreamingManager.Copy((UnicontaBaseEntity)payment, rec);
            deb.ChildRecords = new DebtorTransPayment[] { rec };

            var lan = UtilDisplay.GetLanguage(debtor, api.CompanyEntity);
            rec.Trans.LocOb = Uniconta.ClientTools.Localization.GetLocalization(lan);
            rec._SumAmount = rec.Trans._AmountOpen;
            rec._SumAmountCur = rec.Trans._AmountOpenCur;

            var collectionPrint = await GenerateStandardCollectionReport(debtor, date, deb, debtorEmailType);
            if (collectionPrint == null)
                return null;

            var standardReports = new[] { collectionPrint };
            var standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.CollectionLetter);

            await standardPrint.InitializePrint();
            return standardPrint;
        }

        async private Task<DebtorCollectionReportClient> GenerateStandardCollectionReport(DebtorClient debtor, DateTime dueDate, DebtorPaymentStatementList selectedItem, DebtorEmailType debtorEmailType)
        {
            byte[] logo = await UtilCommon.GetLogo(api);

            var Comp = api.CompanyEntity;
            var companyClient = Comp.CreateUserType<CompanyClient>();
            StreamingManager.Copy(Comp, companyClient);

            var lan = UtilDisplay.GetLanguage(debtor, companyClient);
            string lastMessage;
            var res = await UtilCommon.GetDebtorMessageClient(api, lan, debtorEmailType);
            if (res != null)
                lastMessage = res._Text;
            else
                lastMessage = string.Empty;

            debtor.OpenTransactions = selectedItem.ChildRecords;
            string _reportName = StandardReportUtility.GetLocalizedReportName(debtor, companyClient, debtorEmailType.ToString());

            return new DebtorCollectionReportClient(companyClient, debtor, dueDate, logo, debtorEmailType == DebtorEmailType.InterestNote, _reportName, lastMessage);
        }
    }
}

    


