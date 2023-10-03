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
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EmailSetupPage2 : FormBasePage
    {
        CompanySMTPClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.EmailSetupPage2; } }

        public override Type TableType { get { return typeof(CompanySMTPClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySMTPClient)value; } }
        //For Edit
        public EmailSetupPage2(UnicontaBaseEntity sourceData, bool isEdit) : base(sourceData, isEdit)
        {
            InitializeComponent();
            InitPage(editrow._UseMicrosoftGraph);
        }

        private void InitPage(bool useMicrosoftGraph)
        {
            layoutControl = layoutItems;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow = CreateNew() as CompanySMTPClient;
            }
            layoutItems.DataContext = editrow;
            BusyIndicator = busyIndicator;
            if (!string.IsNullOrEmpty(editrow._Host))
                grpSmtp.IsCollapsed = true;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            cmbExternType.ItemsSource = new List<string> { "Debtor", "DebtorInvoice", "Creditor", "CreditorInvoice", "Employee", "Contact", "CRMCampainMember", "CRMProspect" };
            cmbExternType.SelectedIndex = 0;
            layoutProp.Label = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Properties"));
            liTextinHtml.Label = Uniconta.ClientTools.Localization.lookup("TextInHtml");
            txtSmptPwd.Text = editrow._SmtpPsw;
            if (useMicrosoftGraph)
            {
                rdbGraph.IsChecked = true;
                grpEmailSetup.IsCollapsed = false;
                ShowGraphControls();
            }
            else
                rdbSMTP.IsChecked = true;
            rdbGraph.Checked += rdbGraph_Checked;
        }

      

        public EmailSetupPage2(CrudAPI crudApi, bool useGraph) : base(crudApi, string.Empty)
        {
            InitializeComponent();
            InitPage(useGraph);
        }
        public EmailSetupPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(false);
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
            editrow._SmtpPsw = txtSmptPwd.Text;
            switch (ActionType)
            {
                case "TestMail":
                    if (rdbGraph.IsChecked == true && editrow._TenantId == Guid.Empty)
                        ConfirmAndConnect();
                    else
                    {
                        string title = Uniconta.ClientTools.Localization.lookup("VerifyPOP3");
                        if (editrow._UseMicrosoftGraph)
                            title = Uniconta.ClientTools.Localization.lookup("VerifyGraph");
                        CWCommentsDialogBox dialog = new CWCommentsDialogBox(title, Uniconta.ClientTools.Localization.lookup("Email"));
                        dialog.DialogTableId = 2000000043;
                        dialog.Closing += async delegate
                        {
                            if (dialog.DialogResult == true)
                            {
                                if (!string.IsNullOrEmpty(dialog.Comments) && Utilities.Utility.EmailValidation(dialog.Comments))
                                {
                                    var invapi = new EmailAPI(api);
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
                                    {
                                        if (editrow._UseMicrosoftGraph)
                                            UtilDisplay.ShowErrorCode(err);
                                        else
                                            DebtorEmailSetupPage2.ShowErrorMsg(err, editrow._Host);
                                    }
                                    busyIndicator.IsBusy = false;
                                }
                                else
                                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("InvalidValue"), Uniconta.ClientTools.Localization.lookup("Email"), dialog.Comments), Uniconta.ClientTools.Localization.lookup("Error"));
                            }
                        };
                        dialog.Show();
                    }
                    break;
                case "SetUpEMail":
                    if (rdbGraph.IsChecked == true)
                        return;
                    var objWizardWindow = new WizardWindow(new UnicontaClient.Pages.EmailSetupWizard(), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"),
                    Uniconta.ClientTools.Localization.lookup("EmailSetup")));
                    objWizardWindow.Width = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenWidth) * 0.14;
                    objWizardWindow.Height = System.Convert.ToDouble(System.Windows.SystemParameters.PrimaryScreenHeight) * 0.20;
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
                              editrow._SmtpPsw = txtSmptPwd.Text = emailSetup.Password;
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
                    if (Validate())
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

        bool Validate()
        {
            if (rdbSMTP.IsChecked == true)
                return ValidateSMTP();
            else
                return ValidateGraph();
        }

        bool ValidateGraph()
        {
            if (!editrow._UseMicrosoftGraph || editrow._TenantId == Guid.Empty)
            {
               
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IncompleteGraphAuthorization"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return false;
            }
            else if (string.IsNullOrEmpty(editrow.EmailSendFrom))
            { 
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), liEmailSendFrom.Label), Uniconta.ClientTools.Localization.lookup("Warning"));

            }
            editrow.Host = " ";//manadatory
            return true;
        }
        bool? isSMTPValidated;
        bool ValidateSMTP()
        {
            object element;
            element = FocusManager.GetFocusedElement(UtilDisplay.GetCurentWindow());
            if (element is Control)
            {
                var ctrl = element as Control;
                TraversalRequest tReq = new TraversalRequest(FocusNavigationDirection.Down);
                ctrl.MoveFocus(tReq);
            }
            if (isSMTPValidated == true)
                return true;
            if (editrow.Host == string.Empty)
                editrow.Host = null;
            if (editrow.SmtpUser == string.Empty)
                editrow.SmtpUser = null;
            if (string.IsNullOrEmpty(editrow._SmtpPsw))
            {
                editrow._SmtpPsw = null;
            }
            var loadedRow = this.LoadedRow as CompanySMTPClient;
            if (loadedRow == null && !string.IsNullOrEmpty(editrow.Host))
                isSMTPValidated = false;
            else if (loadedRow != null && ((editrow.Host != null && editrow.Host != loadedRow.Host) || (editrow.Port != 0 && editrow.Port != loadedRow.Port) || (editrow.SmtpUser != null && editrow.SmtpUser != loadedRow.SmtpUser) || (editrow._SmtpPsw != null && editrow._SmtpPsw != loadedRow._SmtpPsw)
                || (editrow.AllowDifferentSender == true && editrow.AllowDifferentSender != loadedRow.AllowDifferentSender)
                || (editrow.AllowDifferentSender == true && editrow.EmailSendFrom != loadedRow.EmailSendFrom)))
                isSMTPValidated = false;
            if (isSMTPValidated == false)
            {
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SMTPVerifyMsg"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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

        private void btnTextInHtml_Click(object sender, RoutedEventArgs e)
        {
            var param = new object[1];
            param[0] = editrow.Body;
            AddDockItem(TabControls.TextInHtmlPage, param, true, Uniconta.ClientTools.Localization.lookup("TextInHtml"), null, new Point(250, 200));
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
                case 6:
                    type = typeof(CrmCampaignMemberClient);
                    break;
                case 7:
                    type = typeof(CrmProspectClient);
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
        IPublicClientApplication app;
        private void rdbGraph_Checked(object sender, RoutedEventArgs e)
        {
            ConfirmAndConnect();
        }
        private async void ConfirmAndConnect()
        {
            ShowGraphControls();
            if (editrow._TenantId == Guid.Empty)
            {
                CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouAzureAdmin"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                dialog.Closing += delegate
                {
                    if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                        Connect(true);
                    else if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.No)
                        Connect(false);

                };
                dialog.Show();
            }
        }
        private async void Connect(bool isAdmin)
        {
            string clientId = "dbcf520d-28cc-4b88-af90-97ca7a761481";
            if (app == null)
            {
                app = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri("https://www.uniconta.com/microsoft-graph-connected")
            .Build();
            }
            var account = await AcquireTokenInteractive(isAdmin).ConfigureAwait(false);
            if (account != null)
            {
                editrow._TenantId = new Guid(account.TenantId);
                editrow._UseMicrosoftGraph = true;
                editrow.EmailSendFrom = account.Account.Username;
                var link = string.Format("https://login.microsoftonline.com/{0}/adminconsent?client_id={1}", account.TenantId, clientId);
                if (!isAdmin)
                {
                    //not showing outlook for now. Not yet finalized
                   // var emailBody = string.Concat(Uniconta.ClientTools.Localization.lookup("GraphMailBody"), string.Format("</br><a href='{0}'>{1}</a>", link, link));
                   // var subject = Uniconta.ClientTools.Localization.lookup("GraphMailSubject");
                   // Utility.DisplayOutLook(string.Empty, string.Empty, string.Empty, emailBody, subject, string.Empty, string.Empty, true);
                }
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UnicontaHyperLinkMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AdminGraphLinkMsg"), link, Uniconta.ClientTools.Localization.lookup("Information"));
                    }));
                //}
                //else
                //{

                //    Dispatcher.BeginInvoke(new Action(() =>
                //    {
                //        var cwBrowserView = new CWBrowserDialog(link);
                //        cwBrowserView.Show();
                //    }));
                //}
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    frmRibbon.SetText("TestMail", Uniconta.ClientTools.Localization.lookup("VerifyGraph"));
                }));
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IncompleteGraphAuthorization"), Uniconta.ClientTools.Localization.lookup("Warning"));
                }));
            }
        }
        private async Task<AuthenticationResult> AcquireTokenInteractive(bool isAdmin)
        {
            //Microsoft.Identity.Client.Prompt prompt = isAdmin ? Microsoft.Identity.Client.Prompt.Consent : Microsoft.Identity.Client.Prompt.NoPrompt;
            List<string> Scopes = new List<string> { "User.read", "Mail.Send"};
            try
            {
                var accounts = (await app.GetAccountsAsync()).ToList();
                var builder = app.AcquireTokenInteractive(Scopes)
                    .WithAccount(accounts.FirstOrDefault())
                    .WithUseEmbeddedWebView(true)
                    .WithPrompt(Microsoft.Identity.Client.Prompt.Consent);
                var result = await builder.ExecuteAsync().ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void rdbSMTP_Checked(object sender, RoutedEventArgs e)
        {
            ShowSMTPControls();
        }
        void ShowGraphControls()
        {
            grpSmtp.Visibility = Visibility.Collapsed;
            frmRibbon.DisableButtons("SetUpEMail");
            if (editrow._TenantId == Guid.Empty)
            {
                var item = frmRibbon.SetText("TestMail", Uniconta.ClientTools.Localization.lookup("Connect"));
                item.LargeGlyph = Utilities.Utility.GetGlyph("SendInvoiceMail_32x32");
            }
            else
            {
                var item = frmRibbon.SetText("TestMail", Uniconta.ClientTools.Localization.lookup("VerifyGraph"));
                item.LargeGlyph = Utilities.Utility.GetGlyph("Tick_32x32");
            }
            liMergeAttachment.Visibility = Visibility.Visible;
            liNameInemail.Visibility = Visibility.Collapsed;
            liReplyTo.Visibility = Visibility.Collapsed;
        }
        void ShowSMTPControls()
        {
            grpSmtp.Visibility = Visibility.Visible;
            frmRibbon.EnableButtons("SetUpEMail");
            frmRibbon.SetText("TestMail", Uniconta.ClientTools.Localization.lookup("VerifyPOP3"));
            liMergeAttachment.Visibility = Visibility.Collapsed;
            liNameInemail.Visibility = Visibility.Visible;
            liReplyTo.Visibility = Visibility.Visible;
        }
    }
}

