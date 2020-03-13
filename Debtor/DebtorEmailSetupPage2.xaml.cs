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
                frmRibbon.DisableButtons( "Delete" );
                editrow = CreateNew() as DebtorEmailSetupClient;
            }
            layoutItems.DataContext = editrow;
            BusyIndicator = busyIndicator;
            if (editrow._host != null || editrow._companySMTP != null)
                grpSmtp.IsCollapsed = true;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            leCompanySMTP.api = api;
            cmbExternType.ItemsSource = new List<string> { "Debtor", "DebtorInvoice", "Creditor", "CreditorInvoice" };
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
                                    UtilDisplay.ShowErrorCode(err);
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
            element = FocusManager.GetFocusedElement(Application.Current.Windows[0]);
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
            if (editrow.Host == string.Empty)
                editrow.Host = null;
            if (editrow.SmtpUser == string.Empty)
                editrow.SmtpUser = null;
            if (string.IsNullOrEmpty(editrow._smtpPassword))
            {
                editrow._smtpPassword = null;
            }
            var loadedRow = this.LoadedRow as DebtorEmailSetupClient;
            if (loadedRow == null && !string.IsNullOrEmpty(editrow.Host))
                isSMTPValidated = false;
            else if (loadedRow != null && ((editrow.Host != null && editrow.Host != loadedRow.Host) || (editrow.Port != 0 && editrow.Port != loadedRow.Port) || (editrow.SmtpUser != null && editrow.SmtpUser != loadedRow.SmtpUser) || (editrow._smtpPassword != null && editrow._smtpPassword != loadedRow._smtpPassword)
                || (editrow.AllowDifferentSender == true && editrow.AllowDifferentSender != loadedRow.AllowDifferentSender)
                || (editrow.AllowDifferentSender == true && editrow.EmailSendFrom != loadedRow.EmailSendFrom)))
                isSMTPValidated = false;
            if (isSMTPValidated == false)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SMTPVerifyMsg"), Uniconta.ClientTools.Localization.lookup("Warning"));
                isSMTPValidated = null;
                return false;
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
            var olSelectionStart = txtEmailBody.SelectionStart;
            var selectedText = Convert.ToString(cmbProperties.SelectedItem);
            string propName = string.Concat("{", cmbExternType.SelectedItem, ".", selectedText, "}");
            if (string.IsNullOrEmpty(txtEmailBody.Text))
                txtEmailBody.Text = propName;
            else
                txtEmailBody.Text = txtEmailBody.Text.Insert(txtEmailBody.SelectionStart, propName);
            txtEmailBody.Focus();
            txtEmailBody.SelectionStart = olSelectionStart + propName.Length;
            txtEmailBody.Select(txtEmailBody.SelectionStart, 0);
            txtEmailBody.Focus();
        }
        private void cmbExternType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            Type type = null;
            var comp = api?.CompanyEntity;

            switch (cmbExternType.SelectedIndex)
            {
                case 0:
                    type = comp?.GetUserType(typeof(DebtorClient)) ?? typeof(DebtorClient);
                    break;
                case 1:
                    type = comp?.GetUserType(typeof(DebtorInvoiceClient)) ?? typeof(DebtorInvoiceClient);
                    break;
                case 2:
                    type = comp?.GetUserType(typeof(CreditorClient)) ?? typeof(CreditorClient);
                    break;
                case 3:
                    type = comp?.GetUserType(typeof(CreditorInvoiceClient)) ?? typeof(CreditorInvoiceClient);
                    break;
            }

            if (type != null)
            {
                List<string> propertyNames = UtilFunctions.GetAllDisplayPropertyNames(type, api.CompanyEntity, false, false);
                cmbProperties.ItemsSource = propertyNames;
                cmbProperties.SelectedIndex = 0;
            }
        }

        private void btnTextInHtml_Click(object sender, RoutedEventArgs e)
        {
            var param = new object[1];
            param[0] = editrow.Body;
            AddDockItem(TabControls.TextInHtmlPage, param, true, Uniconta.ClientTools.Localization.lookup("TextInHtml"), null, new Point(200, 300));
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
#endif
    }
}

