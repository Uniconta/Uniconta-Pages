using Uniconta.API.System;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Util;
using System.Windows;
using Uniconta.ClientTools.Controls;
using Uniconta.API.DebtorCreditor;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using System.Text;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorEmailSetupPage2 : FormBasePage
    {
        private DebtorEmailSetupClient editrow;
        private bool? isSMTPValidated;

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
            layoutProp.Label = $"{Uniconta.ClientTools.Localization.lookup("AddOBJ")} {Uniconta.ClientTools.Localization.lookup("Properties")}";
            liTextinHtml.Label = Uniconta.ClientTools.Localization.lookup("TextInHtml");
            txtSmptPwd.Text = editrow._smtpPassword;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.TextInHtmlPage)
            {
                var html = argument as byte[];
                if (html?.Length > 0)
                {
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
                    TestMailAsync();
                    break;
                case "SetUpEMail":
                    SetUpEmail();
                    break;
                case "Save":
                    if (ValidateSMTP())
                        frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void TestMailAsync()
        {
            var dialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("VerifyPOP3"), Uniconta.ClientTools.Localization.lookup("Email"))
            {
                DialogTableId = 2000000043,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            dialog.Closing += async delegate
            {
                if (dialog.DialogResult == true)
                {
                    if (!string.IsNullOrEmpty(dialog.Comments) && Utilities.Utility.EmailValidation(dialog.Comments))
                    {
                        var invapi = new InvoiceAPI(api);
                        busyIndicator.IsBusy = true;
                        var err = await invapi.TestSMTP(editrow, dialog.Comments);
                        if (err == ErrorCodes.Succes)
                        {
                            UnicontaMessageBox.Show($"{Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ")} {Uniconta.ClientTools.Localization.lookup("Email")}",
                                Uniconta.ClientTools.Localization.lookup("Message"));
                            isSMTPValidated = true;
                            DisableSMTPFields();
                        }
                        else
                        {
                            ShowErrorMsg(err, editrow._host);
                        }
                        busyIndicator.IsBusy = false;
                    }
                    else
                    {
                        UnicontaMessageBox.Show($"{Uniconta.ClientTools.Localization.lookup("InvalidValue")} {Uniconta.ClientTools.Localization.lookup("Email")} {dialog.Comments}", Uniconta.ClientTools.Localization.lookup("Error"));
                    }
                }
            };
            dialog.Show();
        }

        private void SetUpEmail()
        {
            var objWizardWindow = new WizardWindow(new UnicontaClient.Pages.EmailSetupWizard(), $"{Uniconta.ClientTools.Localization.lookup("CreateOBJ")} {Uniconta.ClientTools.Localization.lookup("EmailSetup")}")
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                MinHeight = 120.0d,
                MinWidth = 300.0d
            };

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
                        UnicontaMessageBox.Show($"{Uniconta.ClientTools.Localization.lookup("InvalidValue")} {Uniconta.ClientTools.Localization.lookup("Email")} {emailSetup.User}",
                            Uniconta.ClientTools.Localization.lookup("Warning"));
                    }
                }
            };
            objWizardWindow.Show();
        }

        private bool ValidateSMTP()
        {
            MoveFocusToNextControl();

            if (isSMTPValidated == true)
                return true;

            if (string.IsNullOrEmpty(editrow._host))
                editrow._host = null;
            if (string.IsNullOrEmpty(editrow._smtpUser))
                editrow._smtpUser = null;
            if (string.IsNullOrEmpty(editrow._smtpPassword))
                editrow._smtpPassword = null;

            var loadedRow = LoadedRow as DebtorEmailSetupClient;
            if (loadedRow == null && !string.IsNullOrEmpty(editrow._host))
                isSMTPValidated = false;
            else if (loadedRow != null && IsSMTPSettingsChanged(loadedRow))
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

        private void MoveFocusToNextControl()
        {
            var element = FocusManager.GetFocusedElement(UtilDisplay.GetCurentWindow()) as System.Windows.Controls.Control;
            element?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
        }

        private bool IsSMTPSettingsChanged(DebtorEmailSetupClient loadedRow)
        {
            return (editrow._host != null && editrow._host != loadedRow._host) ||
                   (editrow._port != 0 && editrow._port != loadedRow._port) ||
                   (editrow._smtpUser != null && editrow._smtpUser != loadedRow._smtpUser) ||
                   (editrow._smtpPassword != null && editrow._smtpPassword != loadedRow._smtpPassword) ||
                   (editrow.AllowDifferentSender == true && editrow.AllowDifferentSender != loadedRow.AllowDifferentSender) ||
                   (editrow.AllowDifferentSender == true && editrow.EmailSendFrom != loadedRow.EmailSendFrom);
        }

        private void DisableSMTPFields()
        {
            itemHost.IsEnabled = false;
            itemPort.IsEnabled = false;
            itemSmtpUser.IsEnabled = false;
            itemSmtpPassword.IsEnabled = false;
            itemUseSSL.IsEnabled = false;
        }

        private void InsertProperty_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var selectedText = Convert.ToString(cmbProperties.SelectedItem);
            var propName = $"{{{cmbExternType.SelectedItem}.{selectedText}}}";
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
            var param = new object[1] { editrow._Body };
            AddDockItem(TabControls.TextInHtmlPage, param, true, Uniconta.ClientTools.Localization.lookup("TextInHtml"), null, new System.Windows.Point(250, 150));
        }

        public static async void ShowErrorMsg(ErrorCodes errorCode, string host)
        {
            var lastErrors = await BasePage.session.GetErrors(errorCode);
            var errMsg = UtilDisplay.GetFormattedErrorCode(errorCode, lastErrors);
            var hyperlink = host?.Contains("gmail") == true
                ? "https://www.uniconta.com/unipedia-global/gmail-settings-to-send-mail-in-uniconta/"
                : BasePage.session.User._Language == (byte)Uniconta.Common.Language.da
                    ? "https://www.uniconta.com/da/unipedia/mail_server/"
                    : "https://www.uniconta.com/unipedia-global/mail-server-set-up/";

            UnicontaHyperLinkMessageBox.Show(errMsg, hyperlink,
                lastErrors != null && lastErrors.Length > 0 ? Uniconta.ClientTools.Localization.lookup("Error") : Uniconta.ClientTools.Localization.lookup("Message"));
        }

        private void InsertIntoBody(string inputText)
        {
            var olSelectionStart = txtEmailBody.SelectionStart;
            txtEmailBody.Text = string.IsNullOrEmpty(txtEmailBody.Text) ? inputText : txtEmailBody.Text.Insert(txtEmailBody.SelectionStart, inputText);
            txtEmailBody.Focus();
            txtEmailBody.SelectionStart = olSelectionStart + inputText.Length;
            txtEmailBody.Select(txtEmailBody.SelectionStart, 0);
            txtEmailBody.Focus();
        }

        private void Email_ButtonClicked(object sender)
        {
            if (sender is CorasauLayoutItem layoutItem && layoutItem.Content is TextEditor txtEmail)
            {
                var mail = $"mailto:{txtEmail.Text}";
                var proc = new System.Diagnostics.Process { StartInfo = { FileName = mail } };
                proc.Start();
            }
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

                    var mobilePayUrl = $"https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone={phoneNumber}&amount={{DebtorInvoice.TotalAmount.0.00}}&comment=Faktura{{DebtorInvoice.InvoiceNumber}}&lock=1";
                    if (editrow._Html)
                        mobilePayUrl = $"<a href={mobilePayUrl}>Mobilepay</a>";

                    InsertIntoBody(mobilePayUrl);
                }
            };
            cwTextControl.Show();
        }
    }
}

