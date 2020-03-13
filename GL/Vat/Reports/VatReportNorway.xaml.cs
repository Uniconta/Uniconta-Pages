using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
  

    public class VatReportNorwayGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VatSumOperationReport); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class VatReportNorway : GridBasePage
    {
        #region Member Constants
        protected const string SMSPIN = "SMSPin";
        protected const string ALTINNPIN = "AltinnPin";
        #endregion

        #region Member variables
        protected List<VatSumOperationReport> vatSumOperationLst;
        #endregion

        #region Properties
        private List<VatSumOperationReport> VatSumOperationLst
        {
            get
            {
                return vatSumOperationLst;
            }

            set
            {
                vatSumOperationLst = value;
            }
        }

        private DateTime FromDate { get; set; }
        private DateTime ToDate { get; set; }
        #endregion


        public override string NameOfControl
        {
            get { return TabControls.VatReportNorway; }
        }

        public VatReportNorway(List<VatSumOperationReport> VatSumOperationLst, DateTime fromDate, DateTime toDate) : base(null)
        {
            this.VatSumOperationLst = VatSumOperationLst;
            this.FromDate = fromDate;
            this.ToDate = toDate;

            InitializeComponent();
            this.DataContext = this;
            localMenu.dataGrid = dgVatReportNorway;
            SetRibbonControl(localMenu, dgVatReportNorway);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVatReportNorway.api = api;
            dgVatReportNorway.BusyIndicator = busyIndicator;
#if !SILVERLIGHT
            txtFromDate.DateTime = fromDate;
            txtToDate.DateTime = toDate;
#endif


            VatSumOperationReport post2 = null;
            double sum = 0d, sumBase = 0d;
            foreach (var rec in VatSumOperationLst)
            {
                string s;
                switch(rec._Line)
                {
                    case 1: s = "Samlet omsetning og uttak utenfor merverdiavgiftsloven"; break;
                    case 2: s = "Samlet omsetning og uttak innenfor merverdiavgiftsloven og innførsel";
                            post2 = rec;
                            break;
                    case 3: s = "Innenlands omsetning og uttak, og beregnet avgift 25 %"; break;
                    case 4: s = "Innenlands omsetning og uttak, og beregnet avgift 15 %"; break;
                    case 5: s = "Innenlands omsetning og uttak, og beregnet avgift 10 %"; break;
                    case 6: s = "Innenlands omsetning og uttak fritatt for merverdiavgift"; break;
                    case 7: s = "Innenlands omsetning med omvendt avgiftsplikt"; break;
                    case 8: s = "Utførsel av varer og tjenester fritatt for merverdiavgift"; break;
                    case 9: s = "Innførsel av varer, og beregnet avgift 25 %"; break;
                    case 10: s = "Innførsel av varer, og beregnet avgift 15 %"; break;
                    case 11: s = "Innførsel av varer som det ikke skal beregnes merverdiavgift av"; break;
                    case 12: s = "Tjenester kjøpt fra utlandet, og beregnet avgift 25 %"; break;
                    case 13: s = "Innenlands kjøp av varer og tjenester, og beregnet avgift 25 %"; break;
                    case 14: s = "Fradragsberettiget innenlands inngående avgift 25 %"; break;
                    case 15: s = "Fradragsberettiget innenlands inngående avgift 15 %"; break;
                    case 16: s = "Fradragsberettiget innenlands inngående avgift 10 %"; break;
                    case 17: s = "Fradragsberettiget innførselsmerverdiavgift 25 %"; break;
                    case 18: s = "Fradragsberettiget innførselsmerverdiavgift 15 %"; break;
                    case 19: s = "Avgift å betale / Avgift til gode";
                                rec._Amount = sum;
                                break;
                    default: s = null;break;
                }
                rec._Text = s;
                if (rec._Line >= 3)
                {
                    sum += rec._Amount;
                    if (rec._Line <= 13)
                        sumBase += rec._AmountBase;
                }
            }
            if (post2 != null)
                post2._ExtraBase = sumBase;

            dgVatReportNorway.ItemsSource = VatSumOperationLst;
            dgVatReportNorway.Visibility = Visibility.Visible;
            this.DataContext = null;
            this.DataContext = this;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
#if !SILVERLIGHT
                case "TransferToAltinn":
                    TransferToAltinn(ActionType);
                    break;

                case "GetSMSPinCode":
                    GetAuthCode(ActionType);
                    break;

                case "GetAltinnCode":
                    GetAuthCode(ActionType);
                    break;

                case "AltinnMvaReport":
                    var param = new object[] { api, FromDate, ToDate, VatSumOperationLst };
                    AddDockItem(TabControls.AltinnMvaReport, param, "Mva-oppgaven", closeIfOpened: true); 
                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if !SILVERLIGHT

        private void AltinnResult(AltinnCreationResult result)
        {
            var systemInfo = string.Empty;

            if (result.HasPreCheckErrors)
            {
                foreach (AltinnPrecheckError error in result.PrecheckErrors)
                {
                    systemInfo += error.ToString() + System.Environment.NewLine;
                }
            }
            else
            {
                foreach (AltinnResponse response in result.ResponseInfo)
                {
                    if (response.Error)
                        systemInfo += response.ResponseText + System.Environment.NewLine;
                    else
                        systemInfo += response.ResponseText + System.Environment.NewLine;
                }

                //TODO:This is only for test purpose - will be deleted when Norway has accepted
                //string xmlPath = @"c:\temp\altinn\files\";

                //if (result.AltinnDocument != null)
                //{
                //    var filenameXML = string.Format("{0}Altinn_{1}_{2}.xml", xmlPath, api.CompanyId, DateTime.Now.ToString("yyyyMMddTHHmmss"));
                //    result.AltinnDocument.Save(filenameXML);
                //}

                //if (result.ResponsDocument != null)
                //{
                //    var filenameResponse = string.Format("{0}Response_{1}_{2}.xml", xmlPath, api.CompanyId, DateTime.Now.ToString("yyyyMMddTHHmmss"));
                //    result.ResponsDocument.Save(filenameResponse);
                //}
            }

            txtSystemInfo.Text = systemInfo;
        }

        private void TransferToAltinn(string actionType)
        {
            try
            {
                Altinn altInn = null;
              
                if (txtSMSPinCode.Text == string.Empty && txtAltinnPinCode.Text == string.Empty)
                    altInn = new Altinn(api.CompanyEntity, txtAltinnSystemID.Text, txtAltinnSystemPassword.Text, FromDate, ToDate, ceAltinnTest.IsChecked ?? false);
                else
                {
                    var altinnPinCode = txtSMSPinCode.Text != string.Empty ? txtSMSPinCode.Text : txtAltinnPinCode.Text;
                    var authMethod = txtSMSPinCode.Text != string.Empty ? SMSPIN : ALTINNPIN;

                    altInn = new Altinn(api.CompanyEntity, txtAltinnSystemID.Text, txtAltinnSystemPassword.Text, txtUserSSN.Text, txtUserSSNPassword.Text, altinnPinCode, authMethod, FromDate, ToDate, ceAltinnTest.IsChecked ?? false);
                }

                AltinnResult(altInn.GenerateAltinnXML(VatSumOperationLst));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }


        private void GetAuthCode(string actionType)
        {
            try
            {
                var authMethod = actionType == "GetSMSPinCode" ? SMSPIN : ALTINNPIN;

                Altinn altInn = new Altinn(txtAltinnSystemID.Text, txtAltinnSystemPassword.Text, txtUserSSN.Text, txtUserSSNPassword.Text, authMethod, ceAltinnTest.IsChecked ?? false);

                AltinnResult(altInn.SendAuthenticationRequest());
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }
#endif
    }
}
