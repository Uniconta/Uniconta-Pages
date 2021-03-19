using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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
using Uniconta.ClientTools.Util;
using System.Windows;
using DevExpress.Xpf.Editors.Controls;
using Uniconta.ClientTools.Controls;
using Uniconta.API.DebtorCreditor;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorEmailSetupPage2 : FormBasePage
    {
        DebtorEmailSetupClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorEmailSetupPage2; } }

        public override Type TableType { get { return typeof(DebtorEmailSetupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorEmailSetupClient)value; } }
        //For Edit
        public DebtorEmailSetupPage2(UnicontaBaseEntity sourceData, bool isEdit) : base(sourceData, isEdit)
        {
            InitializeComponent();
            InitPage();
        }

        public DebtorEmailSetupPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            layoutControl = layoutItems;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as DebtorEmailSetupClient;
            }
            layoutItems.DataContext = editrow;
            BusyIndicator = busyIndicator;
            if (editrow._host != null || editrow._companySMTP != null)
                grpSmtp.IsCollapsed = true;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            leCompanySMTP.api = api;
            cmbExternType.ItemsSource = new List<string> { "Debtor", "DebtorInvoice", "Creditor", "CreditorInvoice", "Employee", "Contact" };
            cmbExternType.SelectedIndex = 0;
            layoutProp.Label = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Properties"));
#if !SILVERLIGHT
            liTextinHtml.Label = Uniconta.ClientTools.Localization.lookup("TextInHtml");
#endif
            txtSmptPwd.Text = editrow._smtpPassword;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.TextInHtmlPage)
            {
                var html = argument as byte[];
                if (html.Length > 0)
                {
                    editrow.Body = string.Empty;
                    var htmlBody = Encoding.UTF8.GetString(html, 0, html.Length);
                    editrow.Body = htmlBody?.Replace("\t", string.Empty);
                    editrow.Html = true;
                }
            }
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            editrow._smtpPassword = txtSmptPwd.Text;
            switch (ActionType)
            {
                case "TestMail":
                    CWCommentsDialogBox dialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("VerifyPOP3"), Uniconta.ClientTools.Localization.lookup("Email"));
#if !SILVERLIGHT
                    dialog.DialogTableId = 2000000043;
#endif
                    dialog.Closing += async delegate
                    {
                        if (dialog.DialogResult == true)
                        {
                            if (!string.IsNullOrEmpty(dialog.Comments) && Utilities.Utility.EmailValidation(dialog.Comments))
                            {
                                InvoiceAPI invapi = new InvoiceAPI(api);
                                busyIndicator.IsBusy = true;
                                var err = await invapi.TestSMTP(editrow, dialog.Comments);
                                if (err == ErrorCodes.Succes)
                                {
                                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("Email")),
                                        Uniconta.ClientTools.Localization.lookup("Message"));
                                    isSMTPValidated = true;
                                    itemHost.IsEnabled = false;
                                    itemPort.IsEnabled = false;
                                    itemSmtpUser.IsEnabled = false;
                                    itemSmtpPassword.IsEnabled = false;
                                    itemUseSSL.IsEnabled = false;
                                }
                                else
                                    ShowErrorMsg(err, editrow._host);
                                busyIndicator.IsBusy = false;
                            }
                            else
                                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("InvalidValue"), Uniconta.ClientTools.Localization.lookup("Email"), dialog.Comments), Uniconta.ClientTools.Localization.lookup("Error"));
                        }
                    };
                    dialog.Show();
                    break;
                case "SetUpEMail":
                    var objWizardWindow = new WizardWindow(new UnicontaClient.Pages.EmailSetupWizard(), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"),
                    Uniconta.ClientTools.Localization.lookup("EmailSetup")));
#if !SILVERLIGHT
                    objWizardWindow.Width = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenWidth) * 0.14;
                    objWizardWindow.Height = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenHeight) * 0.20;
#else
                    objWizardWindow.Width = System.Convert.ToDouble(System.Windows.Browser.HtmlPage.Window.Eval("screen.width")) * 0.18;
                    objWizardWindow.Height = System.Convert.ToDouble(System.Windows.Browser.HtmlPage.Window.Eval("screen.height")) * 0.16;
#endif
                    objWizardWindow.MinHeight = 120.0d;
                    objWizardWindow.MinWidth = 350.0d;

                    objWizardWindow.Closed += delegate
                  {
                      if (objWizardWindow.DialogResult == true)
                      {
                          var emailSetup = objWizardWindow.WizardData as ServerInformation;
                          if (!string.IsNullOrEmpty(emailSetup?.User) && Utilities.Utility.EmailValidation(emailSetup?.User))
                          {
                              editrow.Host = emailSetup.Host;
                              editrow.SmtpUser = emailSetup.User;
                              editrow._smtpPassword = txtSmptPwd.Text = emailSetup.Password;
                              editrow.Port = emailSetup.Port;
                              editrow.UseSSL = emailSetup.SSL;
                          }
                          else
                          {
                              UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("InvalidValue"), Uniconta.ClientTools.Localization.lookup("Email"), emailSetup.User),
                                  Uniconta.ClientTools.Localization.lookup("Warning"));
                          }
                      }
                  };
                    objWizardWindow.Show();
                    break;
                case "Save":
                    if (ValidateSMTP())
                    {
                        //if (editrow.Html)
                        //{
                        //    if (!ContainsHTML(editrow.Body))
                        //        editrow.Html = false;
                        //}
                        frmRibbon_BaseActions(ActionType);
                    }
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
        bool? isSMTPValidated;
        bool ValidateSMTP()
        {
            object element;
#if !SILVERLIGHT
            element = FocusManager.GetFocusedElement(UtilDisplay.GetCurentWindow());
            if (element is Control)
            {
                var ctrl = element as Control;
                TraversalRequest tReq = new TraversalRequest(FocusNavigationDirection.Down);
                ctrl.MoveFocus(tReq);
            }
#else
            element = FocusManager.GetFocusedElement();
            if (element is SLTextBox)
            {
                var dp = (element as TextBox).Tag as DateEditor;
                if (dp != null)
                    dp.UpdateEditValueSource();
            }
#endif
            if (isSMTPValidated == true)
                return true;
            if (editrow._host == string.Empty)
                editrow._host = null;
            if (editrow._smtpUser == string.Empty)
                editrow._smtpUser = null;
            if (string.IsNullOrEmpty(editrow._smtpPassword))
            {
                editrow._smtpPassword = null;
            }
            var loadedRow = this.LoadedRow as DebtorEmailSetupClient;
            if (loadedRow == null && !string.IsNullOrEmpty(editrow._host))
                isSMTPValidated = false;
            else if (loadedRow != null && ((editrow._host != null && editrow._host != loadedRow._host) || (editrow._port != 0 && editrow._port != loadedRow._port) || (editrow._smtpUser != null && editrow._smtpUser != loadedRow._smtpUser) || (editrow._smtpPassword != null && editrow._smtpPassword != loadedRow._smtpPassword)
                || (editrow.AllowDifferentSender == true && editrow.AllowDifferentSender != loadedRow.AllowDifferentSender)
                || (editrow.AllowDifferentSender == true && editrow.EmailSendFrom != loadedRow.EmailSendFrom)))
                isSMTPValidated = false;
            if (isSMTPValidated == false)
            {
#if !SILVERLIGHT
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SMTPVerifyMsg"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#else
                if( UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SMTPVerifyMsg"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
#endif
                {
                    FrmRibbon_OnItemClicked("TestMail");
                    return false;
                }
                else
                {
                    isSMTPValidated = null;
                    return false;
                }
            }
            return true;
        }

        private bool ContainsHTML(string CheckString)
        {
            if (string.IsNullOrEmpty(CheckString) || string.IsNullOrWhiteSpace(CheckString)) return false;
            var HtmlString = CheckString.Replace("\r\n", string.Empty);
            Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
            return tagRegex.IsMatch(HtmlString);
        }
        private void InsertProperty_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var selectedText = Convert.ToString(cmbProperties.SelectedItem);
            string propName = string.Concat("{", cmbExternType.SelectedItem, ".", selectedText, "}");

            InsertIntoBody(propName);
        }
        private void cmbExternType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            Type type;
            switch (cmbExternType.SelectedIndex)
            {
                case 0:
                    type = typeof(DebtorClient);
                    break;
                case 1:
                    type = typeof(DebtorInvoiceClient);
                    break;
                case 2:
                    type = typeof(CreditorClient);
                    break;
                case 3:
                    type = typeof(CreditorInvoiceClient);
                    break;
                case 4:
                    type = typeof(EmployeeClient);
                    break;
                case 5:
                    type = typeof(ContactClient);
                    break;
                default: return;
            }

            type = api?.CompanyEntity.GetUserTypeNotNull(type);
            if (type != null)
            {
                cmbProperties.ItemsSource = UtilFunctions.GetAllDisplayPropertyNames(type, api.CompanyEntity, false, false);
                cmbProperties.SelectedIndex = 0;
            }
        }

        private void btnTextInHtml_Click(object sender, RoutedEventArgs e)
        {
            var param = new object[1];
            param[0] = editrow._Body;
            AddDockItem(TabControls.TextInHtmlPage, param, true, Uniconta.ClientTools.Localization.lookup("TextInHtml"), null, new Point(250, 200));
        }

        public static async void ShowErrorMsg(ErrorCodes errorCode, string host)
        {
            var lastErrors = await BasePage.session.GetErrors(errorCode);
            string errMsg = UtilDisplay.GetFormattedErrorCode(errorCode, lastErrors);
            string hyperlink;
            if (host != null && host.Contains("gmail"))
                hyperlink = "https://www.uniconta.com/unipedia-global/gmail-settings-to-send-mail-in-uniconta/";
            else if (BasePage.session.User._Language == (byte)Uniconta.Common.Language.da)
                hyperlink = "https://www.uniconta.com/da/unipedia/mail_server/";
            else
                hyperlink = "https://www.uniconta.com/unipedia-global/mail-server-set-up/";
#if !SILVERLIGHT
            UnicontaHyperLinkMessageBox.Show(errMsg, hyperlink,
                lastErrors != null && lastErrors.Length > 0 ? Uniconta.ClientTools.Localization.lookup("Error") : Uniconta.ClientTools.Localization.lookup("Message"));
#else
            errMsg = string.Concat(errMsg, "\r\n", hyperlink);
            UnicontaMessageBox.Show(errMsg, lastErrors != null && lastErrors.Length > 0 ? Uniconta.ClientTools.Localization.lookup("Error") :
                   Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
#endif
        }

        private void InsertIntoBody(string inputText)
        {
            var olSelectionStart = txtEmailBody.SelectionStart;
            if (string.IsNullOrEmpty(txtEmailBody.Text))
                txtEmailBody.Text = inputText;
            else
                txtEmailBody.Text = txtEmailBody.Text.Insert(txtEmailBody.SelectionStart, inputText);
            txtEmailBody.Focus();
            txtEmailBody.SelectionStart = olSelectionStart + inputText.Length;
            txtEmailBody.Select(txtEmailBody.SelectionStart, 0);
            txtEmailBody.Focus();
        }
#if !SILVERLIGHT

        private void Email_ButtonClicked(object sender)
        {
            var txtEmail = ((CorasauLayoutItem)sender).Content as TextEditor;
            if (txtEmail == null)
                return;
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void InsertMobilePay_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var cwTextControl = new CWTextControl(Uniconta.ClientTools.Localization.lookup("FloatWindow"), Uniconta.ClientTools.Localization.lookup("MobilPhone"));
            cwTextControl.Closed += delegate
            {
                if (cwTextControl.DialogResult == true)
                {
                    var phoneNumber = cwTextControl.InputValue;
                    if (!phoneNumber.All(char.IsNumber))
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"), Uniconta.ClientTools.Localization.lookup("Warning"));
                        return;
                    }

                    string mobilePayUrl = @"https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=" + phoneNumber +
                                          @"&amount={DebtorInvoice.TotalAmount.0.00}&comment=Faktura{DebtorInvoice.InvoiceNumber}&lock=1";
                    if (editrow._Html)
                        mobilePayUrl = @"<a href=" + mobilePayUrl + ">Mobilepay</a>";
                        
                    InsertIntoBody(mobilePayUrl);
                }
            };
            cwTextControl.Show();
        }
#endif
    }
}

