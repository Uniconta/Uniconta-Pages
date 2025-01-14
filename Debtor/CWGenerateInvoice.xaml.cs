using Uniconta.ClientTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.ClientTools.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWGenerateInvoice : ChildWindow
    {
        public string InvoiceNumber { get; set; }
        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get; set; }
        [InputFieldData]
        [Display(Name = "SendInvoiceByEmail", ResourceType = typeof(InputFieldDataText))]
        public bool SendByEmail { get; set; }
        [InputFieldData]
        [Display(Name = "UpdateInventory", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateInventory { get; set; }
        [InputFieldData]
        [Display(Name = "Preview", ResourceType = typeof(InputFieldDataText))]
        public bool ShowInvoice { get; set; }
        [InputFieldData]
        [Display(Name = "GenerateInvoiceOIOUBL", ResourceType = typeof(InputFieldDataText))]
        public bool GenerateOIOUBLClicked { get; set; }
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime GenrateDate { get; set; }
        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string Emails { get; set; }
        [InputFieldData]
        [Display(Name = "SendOnlyToThisEmail", ResourceType = typeof(InputFieldDataText))]
        public bool sendOnlyToThisEmail { get; set; }
        [InputFieldData]
        [Display(Name = "PrintImmediately", ResourceType = typeof(InputFieldDataText))]
        public bool InvoiceQuickPrint { get; set; }
        [InputFieldData]
        [Display(Name = "PostOnlyDelivered", ResourceType = typeof(InputFieldDataText))]
        public bool PostOnlyDelivered { get; set; }
        [InputFieldData]
        [Display(Name = "NumberOfCopies", ResourceType = typeof(InputFieldDataText))]
        public short NumberOfPages { get; set; } = 1;
        [InputFieldData]
        [Display(Name = "SendByOutlook", ResourceType = typeof(InputFieldDataText))]
        public bool SendByOutlook { get; set; }
        [Display(Name = "AdditionalOrders", ResourceType = typeof(InputFieldDataText))]
        public List<object> AdditionalOrders { get; set; }
        [Display(Name = "PhysicalVouchers", ResourceType = typeof(InputFieldDataText))]
        public int PhysicalVoucherRef { get; set; }

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
        private bool invoiceInXml;
        private bool IsSendXmlSalesInvoice;
        public CWGenerateInvoice(bool showSimulation, string title, bool showInputforInvNumber, bool askForEmail, bool showInvoice, bool isShowInvoiceVisible, bool showNoEmailMsg, string debtorName,
            bool isShowUpdateInv, bool isOrderOrQuickInv, bool isQuickPrintVisible, bool isDebtorOrder, bool InvoiceInXML, bool isPageCountVisible)
            : this(showSimulation, title, showInputforInvNumber, askForEmail, showInvoice, isShowInvoiceVisible, showNoEmailMsg, debtorName, isShowUpdateInv, isOrderOrQuickInv, isQuickPrintVisible, isDebtorOrder, InvoiceInXML, isPageCountVisible, null)
        {
        }

        public CWGenerateInvoice(bool showSimulation = true, string title = "", bool showInputforInvNumber = false, bool askForEmail = false, bool showInvoice = true, bool isShowInvoiceVisible = true, bool showNoEmailMsg = false, string debtorName = "",
            bool isShowUpdateInv = false, bool isOrderOrQuickInv = false, bool isQuickPrintVisible = true, bool isDebtorOrder = false, bool InvoiceInXML = false, bool isPageCountVisible = true, string AccountName = null)
        {
            this.DataContext = this;
            this.invoiceInXml = InvoiceInXML;
            InitializeComponent();
            ShowInvoice = true;
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateInvoice");
            if (isOrderOrQuickInv)
            {
                lgUBL.Visibility = Visibility.Visible;
                chkOIOUBL.IsEnabled = true;
                chkOIOUBL.IsChecked = InvoiceInXML;
            }
            dpDate.DateTime = dpDate.DateTime == DateTime.MinValue ? BasePage.GetSystemDefaultDate() : dpDate.DateTime;

            if (AccountName == null)
                lgAccount.Visibility = Visibility.Collapsed;
            else
                txtAccountName.Text = AccountName;

            if (!showSimulation)
            {
                this.IsSimulation = false;
                liIsSimulation.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(title))
                    this.Title = Uniconta.ClientTools.Localization.lookup(title);
            }
            if (!showInputforInvNumber)
            {
                liInvoiceNumber.Visibility = Visibility.Collapsed;
                txtInvNumber.Text = string.Empty;
            }
            if (!isShowUpdateInv)
                liUpdateInventory.Visibility = Visibility.Collapsed;
            if (!isDebtorOrder)
                liPostOnlyDelivered.Visibility = Visibility.Collapsed;
            if (askForEmail)
            {
                if (showNoEmailMsg)
                {
                    txtNoMailMsg.Text = string.Format(Uniconta.ClientTools.Localization.lookup("DebtorHasNoEmail"), debtorName);
                    chkSendEmail.IsEnabled = false;
                }
                else
                {
                    liNoEmailMsg.Visibility = txtNoMailMsg.Visibility = Visibility.Collapsed;
                    chkSendEmail.IsChecked = true;
                }
            }
            else
            {
                liSendByEmail.Visibility = Visibility.Collapsed;
                txtNoMailMsg.Text = string.Empty;
                txtInvNumber.Text = string.Empty;
            }

            //Code added to set the correct label for purchase invoice or Purchase pack note
            CompanyLayoutType layoutType;
            if (Enum.TryParse(title, out layoutType) && layoutType == CompanyLayoutType.PurchasePacknote)
                liInvoiceNumber.Label = Uniconta.ClientTools.Localization.lookup("PackNoteNumber");
            else
                liInvoiceNumber.Label = Uniconta.ClientTools.Localization.lookup("InvoiceNumber");
            txtInvNumber.MaxLength = 20;
            chkShowInvoice.IsChecked = showInvoice;
            if (!isShowInvoiceVisible)
                liShowInvoice.Visibility = Visibility.Collapsed;
            lgPrint.Visibility = isQuickPrintVisible ? Visibility.Visible : Visibility.Collapsed;
            liNumberOfPages.Visibility = isQuickPrintVisible && isPageCountVisible ? Visibility.Visible : Visibility.Collapsed;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsSendXmlSalesInvoice)
                liGenerateOIOUBLClicked.Label = Uniconta.ClientTools.Localization.lookup("SendInvoicebyUBL");
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        public void SetSendAsEmailCheck(bool isChecked)
        {
            chkSendEmail.IsChecked = isChecked;
        }

        public void SetInvoiceNumber(long Number)
        {
            SetInvoiceNumber(NumberConvert.ToStringNull(Number));
        }

        public void SetInvoiceNumber(string Number)
        {
            InvoiceNumber = Number;
            txtInvNumber.Text = Number;
        }
        public void SetInvoiceDate(DateTime date)
        {
            dpDate.DateTime = date;
        }

        public void SetInvPrintPreview(bool InvPrintPrvw)
        {
            chkShowInvoice.IsChecked = InvPrintPrvw;
        }

        public void SetAdditionalOrders(IEnumerable<DCOrder> orderList)
        {
            SetAdditionalOrders(orderList, null);
        }
        public void SetAdditionalOrders(IEnumerable<DCOrder> orderList, IEnumerable<object> selectedItems)
        {
            lgOrders.Visibility = Visibility.Visible;
            cbOrders.ItemsSource = orderList;
            cbOrders.EditValue = selectedItems;
        }

        CreditorOrderClient _dcOrder;
        CrudAPI _api;
        public void SetVouchersFromCreditorOrder(CrudAPI api, CreditorOrderClient dcOrder)
        {
            if (lgOrders.Visibility == Visibility.Collapsed)
            {
                lgOrders.Visibility = Visibility.Visible;
                liAdditionalOrders.Visibility = Visibility.Collapsed;
            }
            liDocumentRef.Visibility = Visibility.Visible;
            _dcOrder = dcOrder;
            _api = api;
        }

        public void SetOIOUBLLabelText(bool sendXmlSalesInvoice) { } //Method to backward compatible


        public void SentByEInvoice(CrudAPI api, Tuple<NHRNetworkType, NHREndPointType, string> endPoint, bool forceEnableEinvoice = false)
        {
            IsSendXmlSalesInvoice = true;
            var enableEinvoice = api.CompanyEntity._OIOUBLSendOnServer && invoiceInXml && (endPoint == null || endPoint.Item3 != null);
            chkOIOUBL.IsChecked = enableEinvoice;
            liGenerateOIOUBLClicked.IsEnabled = enableEinvoice || forceEnableEinvoice;

            if (!enableEinvoice || endPoint == null)
                return;
            
            liReceiverEndPoint.Visibility = Visibility.Visible;
            var endPointId = Regex.Replace(endPoint.Item3, "[^0-9]", "");
            var baseUrl = api.session.Connection.Target == APITarget.Live ? NHR.NHR_WEB : NHR.NHR_WEB_DEMO;
            var keyValue = endPoint.Item2 == NHREndPointType.GLN ? "&key=" : "DK%3ACVR&key=";
            var NHRUrl = string.Concat(baseUrl, keyValue, endPointId);

            if (endPoint.Item1 == NHRNetworkType.Peppol)
                lblReceiverEndPoint.Content = string.Concat("Peppol", Environment.NewLine, Uniconta.ClientTools.Localization.lookup(endPoint.Item2 == NHREndPointType.GLN ? "GLNnumber" : "CompanyRegNo"),": ", endPoint.Item3);
            else
                lblReceiverEndPoint.Content = UtilDisplay.CreateHyperLinkTextControl(NHRUrl, string.Concat(Uniconta.ClientTools.Localization.lookup(endPoint.Item2 == NHREndPointType.GLN ? "GLNnumber" : "CompanyRegNo"), ": ", endPointId));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var str = txtInvNumber.Text;
            if (!string.IsNullOrEmpty(str) && str != "0")
            {
                if (str.Length <= 20)
                    InvoiceNumber = str;
                else
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NumberTooBig"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }
            }

            SendByEmail = chkSendEmail.IsChecked.GetValueOrDefault();
            IsSimulation = chkSimulation.IsChecked.GetValueOrDefault();
            GenrateDate = dpDate.DateTime;
            ShowInvoice = chkShowInvoice.IsChecked.GetValueOrDefault();
            UpdateInventory = chkUpdateInv.IsChecked.GetValueOrDefault();
            InvoiceQuickPrint = chkPrintInvoice.IsChecked.GetValueOrDefault();
            GenerateOIOUBLClicked = chkOIOUBL.IsChecked.GetValueOrDefault();
            Emails = (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrWhiteSpace(txtEmail.Text)) ? null : txtEmail.Text;
            sendOnlyToThisEmail = chkSendOnlyEmail.IsChecked.GetValueOrDefault();
            PostOnlyDelivered = chkPostOnlyDel.IsChecked.GetValueOrDefault();
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void chkShowInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkPrintInvoice.IsChecked = false;
        }

        private void chkPrintInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkShowInvoice.IsChecked = false;
        }

        private void chkSendEmail_Checked(object sender, RoutedEventArgs e)
        {
            chkSendOutlook.IsChecked = false;
        }

        private void chkSendOutlook_Checked(object sender, RoutedEventArgs e)
        {
            chkSendEmail.IsChecked = false;
        }

        public void HideOutlookOption(bool isHidden)
        {
            if (isHidden)
                liSendByOutlook.Visibility = Visibility.Collapsed;
        }

        async private void liDocumentRef_LookupButtonClicked(object sender)
        {
            var lookupDocumentRefEditor = sender as LookupEditor;
            lookupDocumentRefEditor.PopupContentTemplate = (Application.Current).Resources["LookUpUrlDocumentClientPopupContent"] as ControlTemplate;
            lookupDocumentRefEditor.ValueMember = "RowId";
            lookupDocumentRefEditor.SelectedIndexChanged += LookupDocumentRefEditor_SelectedIndexChanged;
            var voucherList = await Utilities.Utility.GetVoucherReferenceList(_api, _dcOrder);
            lookupDocumentRefEditor.ItemsSource = voucherList;
        }

        private void LookupDocumentRefEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookUpEditor = sender as LookupEditor;
            var voucherClient = lookUpEditor.SelectedItem as VouchersClient;
            PhysicalVoucherRef = voucherClient.RowId;
            NotifyPropertyChanged(nameof(PhysicalVoucherRef));
        }
    }
}

