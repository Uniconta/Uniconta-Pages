using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatReportIcelandGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(VatSumOperationReport); }
        }

        public override bool Readonly
        {
            get { return true; }
        }
    }

    public partial class VatReportIceland : GridBasePage
    {
        #region Member Constants

        #endregion

        #region Member variables

        protected List<VatSumOperationReport> vatSumOperationLst;

        #endregion

        #region Properties

        private List<VatSumOperationReport> VatSumOperationLst
        {
            get { return vatSumOperationLst; }

            set { vatSumOperationLst = value; }
        }

        private DateTime FromDate { get; }
        private DateTime ToDate { get; }

        #endregion

        public override string NameOfControl
        {
            get { return "VatReportIceland"; }
        }

        readonly VatReport owner; // the VAT report it was opened from, which saves the settlement

        public VatReportIceland(List<VatSumOperationReport> VatSumOperationLst, DateTime fromDate, DateTime toDate) : this(VatSumOperationLst, fromDate, toDate, null) { }

        public VatReportIceland(List<VatSumOperationReport> VatSumOperationLst, DateTime fromDate, DateTime toDate, VatReport owner) : base(null)
        {
            this.owner = owner;
            this.VatSumOperationLst = VatSumOperationLst;
            this.FromDate = fromDate;
            this.ToDate = toDate;
            InitializeComponent();
            dgVatReportIceland.BusyIndicator = busyIndicator;
            dgVatReportIceland.api = api;
            localMenu.dataGrid = dgVatReportIceland;
            SetRibbonControl(localMenu, dgVatReportIceland);
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            double sum = 0d;
            foreach (var rec in VatSumOperationLst)
            {
                string s;
                switch (rec._Line)
                {
                    case 1:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatTaxableHigh");
                        break;
                    case 2:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatTaxableLow");
                        break;
                    case 3:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatExempt");
                        break;
                    case 4:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatOutputHigh");
                        break;
                    case 5:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatOutputLow");
                        break;
                    case 6:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatInputHigh");
                        break;
                    case 7:
                        s = Uniconta.ClientTools.Localization.lookup("IcelandVatInputLow");
                        break;
                    default:
                        s = null;
                        break;
                }

                rec._Text = s;
                if (rec._Line >= 6)
                    sum += rec._Amount;
                else if (rec._Line >= 4) sum -= rec._Amount;
            }
            dgVatReportIceland.ItemsSource = VatSumOperationLst;
            txtSystemInfo.Text = string.Format(Uniconta.ClientTools.Localization.lookup(sum < 0 ? "IcelandVatEstimatedPayable" : "IcelandVatEstimatedRefund"), Math.Abs(sum).ToString("C0"));
            dgVatReportIceland.Visibility = Visibility.Visible;
#if !SILVERLIGHT
            txtFromDate.DateTime = fromDate;
            txtToDate.DateTime = toDate;
            txtKennitala.Text = api.CompanyEntity._Id;
            txtVatNo.Text = api.CompanyEntity._VatNumber;
            txtKennitala.IsReadOnly = txtVatNo.IsReadOnly = true; // the server files with the company's own
#endif
        }

        private async void localMenu_OnItemClicked(string ActionType)
        {
            var previousContent = busyIndicator.BusyContent;

            switch (ActionType)
            {
                case "SendToRSK":
                    await SendToRSK();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = previousContent;
        }

        // The server files the saved settlement of the period (VatAPI.Upload), so it must be saved first
        private async Task SendToRSK()
        {
            var settlement = new GLVatReported() { _ToDate = ToDate };
            if (await api.Read(settlement) != ErrorCodes.Succes || settlement._FromDate == DateTime.MinValue)
            {
                if (owner == null)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandVatSaveBeforeFiling"), Uniconta.ClientTools.Localization.lookup("ReportTo"));
                    return;
                }
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SaveChanges"), Uniconta.ClientTools.Localization.lookup("ReportTo"), MessageBoxButton.YesNo) != MessageBoxResult.Yes ||
                    !await owner.SaveVatReportNow())
                    return;
                settlement = new GLVatReported() { _ToDate = ToDate };
                if (await api.Read(settlement) != ErrorCodes.Succes || settlement._FromDate == DateTime.MinValue)
                    return;
            }
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;
            var receipt = await IcelandVatFiling.File(api, settlement, txtPassword.Text);
            busyIndicator.IsBusy = false;
            if (receipt != null)
                DisplaySavedPdf(receipt);
        }

        private void DisplaySavedPdf(byte[] pdfBytes)
        {
            var ms = new MemoryStream(pdfBytes);
            pdfViewer.DocumentSource = ms;
            var controlWidth = gridColumn.Width;
            gridColumn.Width = pdfColumn.Width;
            pdfColumn.Width = controlWidth;
        }
    }
}
