using UnicontaClient.Models;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Utilities;
using UnicontaClient.Pages;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls.Reporting;
using UnicontaClient.Controls.Dialogs;
using System.Linq;
using Uniconta.Reports.Utilities;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.Reports;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppUserReportDevExpressGrid : CorasauDataGridClient
    {
        public int reportversion;
        public override Type TableType
        {
            get
            {
                switch (reportversion)
                {
                    case 10: return typeof(AppStandardStatementReportClient);
                    case 11: return typeof(AppStandardCollectionReportClient);
                    case 12: return typeof(AppStandardStatementCurrencyReportClient);
                    case 13: return typeof(AppStandardInvoiceReportClient);
                    case 15: return typeof(AppStandardInterestNoteReportClient);
                    case 16: return typeof(AppStandardQuotationReportClient);
                    case 17: return typeof(AppStandardOrderConfirmationClient);
                    case 18: return typeof(AppStandardPackNoteClient);
                    case 19: return typeof(AppStandardRequisitionClient);
                    case 20: return typeof(AppStandardPurchaseOrderClient);
                    case 21: return typeof(AppStandardPurchasePackNoteClient);
                    case 22: return typeof(AppStandardCollectionCurrencyClient);
                    case 23: return typeof(AppStandardInterestNoteCurrencyClient);
                    case 24: return typeof(AppStandardPurchaseInvoiceClient);
                    case 25: return typeof(AppStandardSalesPickingListClient);
                    case 26: return typeof(AppStandardInvTransferOrderClient);
                    case 27: return typeof(AppStandardInvTransferPickListClient);
                    case 50: return typeof(AppStandardBalanceFrontPageClient);
                    case 75: return typeof(AppStandardProductionOrderClient);
                    case 99: return typeof(AppStandardProjectEstimateClient);
                    default: return typeof(AppReportDevExpressClient);
                }
            }
        }
        public override bool Readonly { get { return true; } }
    }

    /// <summary>
    /// Interaction logic for AppReportsPage.xaml
    /// </summary>
    public partial class AppReportsPage : GridBasePage
    {
        ReportGeneratorInitializer rgInstance;
        public AppReportsPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            InitializePage(null);
        }

        public AppReportsPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitializePage(master);
        }

        void InitializePage(UnicontaBaseEntity master)
        {
            var masterlist = new List<UnicontaBaseEntity>();
            if (master != null)
                masterlist.Add(master);
            masterlist.Add(Utility.GetCurrentApp());
            dgUserReportDevX.masterRecords = masterlist;
            Layout._SubId = api.CompanyId;
            dgUserReportDevX.api = api;
            dgUserReportDevX.BusyIndicator = busyIndicator;
            dgUserReportDevX.reportversion = 1;
            SetRibbonControl(localMenu, dgUserReportDevX);
            var reports = Utility.GetReportTypes();
            cmbReports.ItemsSource = reports;
            dgUserReportDevX.Visibility = Visibility.Visible;
            dgUserReportDevX.RowDoubleClick += DgUserReportDevX_RowDoubleClick;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        public override Task InitQuery()
        {
            return null;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == Uniconta.ClientTools.UnicontaTabs.AppReportsPage2)
                dgUserReportDevX.UpdateItemSource(argument);
        }

        private void DgUserReportDevX_RowDoubleClick()
        {
            var selectedCmbItem = cmbReports.SelectedItem as UnicontaReports;

            if (selectedCmbItem.ReportVersion == 1)
                ribbonControl.PerformRibbonAction("ViewReport");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserReportDevX.SelectedItem as AppReportDevExpressClient;
            switch (ActionType)
            {
                case "ViewReport":
                    if (selectedItem != null)
                        AddDockItem(Uniconta.ClientTools.UnicontaTabs.ReportsPreviewPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), selectedItem._Name));
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRights"), Uniconta.ClientTools.Localization.lookup("Information"));
                    break;

                //case "EditRow":
                //    if (selectedItem != null)
                //        LoadReport(selectedItem);
                //    break;
                //case "AddRow":
                //    AddReport();
                //    break;
                //case "CopyRow":
                //    if (selectedItem != null)
                //    {
                //        var selectedReport = cmbReports.SelectedItem as UnicontaReports;
                //        if (selectedReport != null)
                //        {
                //            var stReportVer = selectedReport.ReportVersion;

                //            if (stReportVer != 75)
                //            {
                //                var cwCopyReport = stReportVer > 1 ? new CWCopyReport((StandardReports)stReportVer) : new CWCopyReport();
                //                cwCopyReport.Closed += delegate
                //                {
                //                    if (cwCopyReport.DialogResult == true)
                //                        CopyReport(cwCopyReport.SelectedReport, selectedItem, cwCopyReport.ReportName);
                //                };
                //                cwCopyReport.ShowDialog();
                //            }
                //            else
                //            {
                //                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("MaxProductionOrderReport"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                //                return;
                //            }
                //        }
                //    }
                //    else
                //        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"),
                //            MessageBoxButton.OK);
                //    break;
                case "RefreshGrid":
                    Filter(null);
                    break;
                case "EditSettings":
                    if (selectedItem == null) return;
                    AddDockItem(TabControls.AppReportsPage2, selectedItem);
                    break;
                //case "EditOwner":
                //    if (selectedItem == null) return;
                //    ModifyOwner(selectedItem);
                //    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        //private void ModifyOwner(UserReportDevExpressClient selectedItem)
        //{
        //    var userReportApi = new Uniconta.API.System.UserReportAPI(api);
        //    var cwInputTextCtrl = new CWTextControl(string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Owner")), "LoginId");
        //    cwInputTextCtrl.Closed += async delegate
        //     {
        //         if (cwInputTextCtrl.DialogResult == true)
        //         {
        //             var result = await userReportApi.ChangeOwner(selectedItem, cwInputTextCtrl.InputValue);

        //             if (result != ErrorCodes.Succes)
        //                 UtilDisplay.ShowErrorCode(result);
        //             else
        //                 await Filter(null);
        //         }
        //     };
        //    cwInputTextCtrl.Show();
        //}

        //async void LoadReport(AppReportDevExpressClient report)
        //{
        //    busyIndicator.IsBusy = true;
        //    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
        //    await api.Read(report);
        //    globalEvents.NotifyLoadAppReport(report);
        //    busyIndicator.IsBusy = false;
        //}

        //void AddReport()
        //{
        //    var selectedReport = cmbReports.SelectedItem as UnicontaReports;
        //    if (selectedReport == null)
        //        return;
        //    var reportVersion = selectedReport.ReportVersion;

        //    if (reportVersion != 75 && reportVersion != 26 && reportVersion != 27)
        //        globalEvents.NotifyAddReport(reportVersion);
        //    else
        //    {
        //        var items = dgUserReportDevX.VisibleItems;

        //        if (items.Count > 0)
        //        {
        //            var reports = items.Cast<UserReportDevExpressClient>();
        //            bool canAddReport = true;
        //            foreach (var report in reports)
        //            {
        //                if (report.ForCompanyId == api.CompanyEntity?.CompanyId || report.AllCompanies)
        //                {
        //                    canAddReport = false;
        //                    break;
        //                }
        //            }

        //            if (!canAddReport)
        //            {
        //                var rep = ((StandardReports)reportVersion).ToString();
        //                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MaxOrderReportOBJ"),Uniconta.ClientTools.Localization.lookup(rep)), 
        //                    Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
        //                return;
        //            }
        //        }
        //        globalEvents.NotifyAddReport(reportVersion);
        //    }
        //}

        private void BindGrid(int report)
        {
            if (report > 0)
                dgUserReportDevX.reportversion = report;

            Filter(null);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgUserReportDevX.Filter(propValuePair);
        }

        public override string NameOfControl { get { return Uniconta.ClientTools.UnicontaTabs.AppReportsPage; } }

        private void cmbReports_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedCmbItem = cmbReports.SelectedItem as UnicontaReports;

            if (selectedCmbItem != null)
            {
                BindGrid(selectedCmbItem.ReportVersion);

                if (selectedCmbItem.ReportVersion > 1)
                    ribbonControl.DisableButtons(new string[] { "ViewReport" });
                else
                    ribbonControl.EnableButtons(new string[] { "ViewReport" });
            }
        }



        //async private void CopyReport(UnicontaReports copyToReport, UserReportDevExpressClient copyFromReport, string newreportName)
        //{
        //    try
        //    {
        //        busyIndicator.IsBusy = true;
        //        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");

        //        await api.Read(copyFromReport);
        //        var data = copyFromReport._Layout;
        //        var reportVersion = copyToReport.ReportVersion;

        //        UserReportDevExpressClient client = Activator.CreateInstance(copyToReport.ReportClientType) as UserReportDevExpressClient;
        //        StreamingManager.Copy(copyFromReport, client);

        //        if (reportVersion > 1 && reportVersion < 100)
        //        {
        //            string[] reportInfoObj;
        //            var standardReport = ReportUtil.GetXtraReportFromLayout(data, out reportInfoObj);

        //            if (standardReport == null) return;

        //            if (rgInstance == null)
        //                rgInstance = ReportGeneratorInitializer.Initialize(session, api.CompanyEntity, false);

        //            if (rgInstance == null || rgInstance.ReportTableTypes == null || rgInstance.ReportTableTypes.Count == 0)
        //                return;

        //            var reportInfoObject = ReportUtil.CreateReportInfoObject(reportInfoObj, rgInstance.ReportTableTypes);
        //            reportInfoObject.ReportVersion = copyToReport.ReportVersion;
        //            client._Layout = ReportUtil.StreamReportInDb(standardReport, null, null, reportInfoObj, reportInfoObject.ReportLayoutFormat == 0 ? false : true);
        //        }

        //        client._Name = newreportName;
        //        api.Insert(client);

        //        busyIndicator.IsBusy = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
        //        busyIndicator.IsBusy = false;
        //    }

        //}
    }
}
