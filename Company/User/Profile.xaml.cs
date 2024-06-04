using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using Corasau.Admin.API;
using Uniconta.WindowsAPI.Service;
using UnicontaAPI.Project.API;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class Profile : FormBasePage
    {
        UserClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            BasePage.session.User._ClosePageOnEsc = editrow.ClosePageOnEsc;
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(UserClient); } }
        public override string NameOfControl { get { return TabControls.Profile; } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (UserClient)value; } }
        byte currentTheme;
        byte Curlanguage;
        public Profile(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);

        }
        public Profile(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }
        void InitPage(CrudAPI crudapi)
        {
            this.DataContext = this;
            ShowXapBuildDate();
            layoutControl = layoutItems;
            txtDotnetVersion.Text = System.Environment.Version.ToString();
            if (LoadedRow == null)
            {
                editrow = CreateNew() as UserClient;
                cbUserNationality.SelectedIndex = 0;
            }
            layoutItems.DataContext = editrow;
            currentTheme = editrow._Theme;
            Curlanguage = editrow._Language;

            cbDefaultPrinter.ItemsSource = UtilDisplay.GetInstalledPrinters();
            CheckTwoFactorLogin(session.TwoFactorLoginUsed);
            RemoveMenu();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void CheckTwoFactorLogin(bool twoFactorLoginUsed)
        {
            if (!twoFactorLoginUsed)
                frmRibbon.DisableButtons("RemoveTwoFactorLogin");
        }
        private void RemoveMenu()
        {
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "LatestXap");
        }
        public static string AssemblyBuildDate(Type type)
        {
            string versionText = type.Assembly.FullName.Split(',')[1].Trim().Split('=')[1];
            int iDays = Convert.ToInt32(versionText.Split('.')[2]);
            DateTime refDate = new DateTime(2000, 1, 1);
            DateTime buildDate = refDate.AddDays(iDays);
            int iSeconds = Convert.ToInt32(versionText.Split('.')[3]);
            iSeconds = iSeconds * 2;
            buildDate = buildDate.AddSeconds(iSeconds);
            return buildDate.ToString();

        }

        private void ShowXapBuildDate()
        {
            string buildText = AssemblyBuildDate(this.GetType());
            txtXapBuildDate.Text = buildText;
            txtClientVersion.Text = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            txtAPIVersion.Text = APIVersion.CurrentVersion.ToString();
        }
        private async void savePassword()
        {
            var err = await session.ChangePassword(txtOldPassword.Password, txtPassword.Password);
            if (err == ErrorCodes.Succes)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PasswordChanged"), Uniconta.ClientTools.Localization.lookup("Information"));
            else
                UtilDisplay.ShowErrorCode(err);
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (!string.IsNullOrEmpty(txtPassword.Password))
                    {
                        if (txtPassword.Password != txtConfirmPassword.Password)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PasswordMismatch"), Uniconta.ClientTools.Localization.lookup("Error"));
                            return;
                        }
                        savePassword();
                    }
                    Uniconta.ClientTools.Controls.LookupEditor.DefaultImmediatePopup = editrow._AutoDropDown;
                    frmRibbon_BaseActions(ActionType);

                    var user = api.session.User;
                    user._ClosePageOnEsc = editrow._ClosePageOnEsc;
                    user._AllowMathExpression = editrow._AllowMathExpression;
                    user._AutoDropDown = editrow._AutoDropDown;
                    user._ColumnFilter = editrow._ColumnFilter;
                    user._AppDocPath = editrow._AppDocPath;
                    user._ShowGridLines = editrow._ShowGridLines;
                    user._ConfirmDelete = editrow._ConfirmDelete;
                    user._AutoSave = editrow._AutoSave;
                    user._UseDefaultBrowser = editrow._UseDefaultBrowser;
                    //user._IncludeFilterInLayout = editrow._IncludeFilterInLayout;
                    string msg;
                    if (Curlanguage != editrow._Language)
                        msg = string.Format(Uniconta.ClientTools.Localization.lookup("LanguageApplyMsg"), editrow.UserLanguage);
                    else
                        msg = null;
                    if (currentTheme != editrow._Theme)
                    {
                        var msg2 = string.Format(Uniconta.ClientTools.Localization.lookup("ThemeApplyMsg"), editrow.Theme);
                        if (msg != null)
                            msg = msg + '\n' + msg2;
                        else
                            msg = msg2;
                    }
                    if (msg != null)
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                    break;
                case "CopyLayout":
                    CopyLayout();
                    break;
                case "Subscription":
                    Subscripe();
                    break;
                case "UserOperations":
                    AddDockItem(TabControls.UserOperationsLog, editrow, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserOperations"), editrow._Name));
                    break;
                case "ActiveSessions":
                    AddDockItem(TabControls.ActiveSessionsPage, null, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ActiveSessions"), editrow._Name));
                    break;
                case "LatestXap":
                    break;
                case "UserLayout":
                    AddDockItem(TabControls.UserLayoutPage, null, Uniconta.ClientTools.Localization.lookup("UserLayout"));
                    break;
                case "UserLoginHistory":
                    AddDockItem(TabControls.AllUsersLoginHistoryPage, api.session.User, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserLoginHistory"), editrow._Name));
                    break;
                case "PasswordOnEmail":
                    SetEmailPassowrd();
                    break;
                case "ProgramModules":
                    AddDockItem(TabControls.AppKeyPage, api.session.User, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("AppKeys"), editrow._Name));
                    break;
                case "RemoveTwoFactorLogin":
                    RemoveTwoFactorLogin();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
        
        async void RemoveTwoFactorLogin()
        {
            var userApi = new TwoFactorLogin(api.session);
            var result = await userApi.RemoveTwoFactorLogin();
            if (result == ErrorCodes.Succes)
            {
                session.TwoFactorLoginUsed = false;
                CheckTwoFactorLogin(false);
            }
            UtilDisplay.ShowErrorCode(result);
        }

        async void SetEmailPassowrd()
        {
            var employee = await api.Query<Uniconta.DataModel.Employee>(editrow);
            if (employee.FirstOrDefault() != null)
            {
                var cw = new CwSetEmailPassword(employee.FirstOrDefault() as Uniconta.DataModel.Employee);
                cw.Closing += async delegate
                 {
                     if (cw.DialogResult == true)
                     {
                         var empApi = new EmployeeAPI(api);
                         var result = await empApi.SetPasswordOnEmployee(cw.Password);
                         UtilDisplay.ShowErrorCode(result);
                     }
                 };
                cw.Show();
            }
            else
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("UserNotEmployee"), editrow.Name), Uniconta.ClientTools.Localization.lookup("Error"));
        }

        public static async void DownloadLatestXap(CrudAPI api, System.Windows.Threading.Dispatcher Dispatcher)
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    var logapi = new Corasau.Admin.API.ServerlogAPI(api);
                    int downloadSize = 2000000;//2 mb
                    var buffer = await logapi.Download("xap", downloadSize);
                    if (buffer == null)
                        Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(logapi.LastError);
                    else
                    {
                        try
                        {
                            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                            path = path.Replace("/Documents", "");
                            path = string.Format("{0}/{1}", path, "Library/Application Support/Microsoft/Silverlight/OutOfBrowser/2833077486.erp.uniconta.com/");
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (!Directory.Exists(path))
                                {
                                    UnicontaMessageBox.Show("Path not found." + path, Uniconta.ClientTools.Localization.lookup("Error"));
                                    return;
                                }
                            }));
                            using (var file = File.Create(path + "application.xap"))
                            {
                                buffer.CopyTo(file);
                            }
                            buffer.Release();

                            Dispatcher.BeginInvoke(new Action(() => { UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SlUpdate"), Uniconta.ClientTools.Localization.lookup("Message")); }));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.BeginInvoke(new Action(() => { UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception")); }));
                            api.ReportException(ex, string.Format("DownloadLatestXap, CompanyId={0}", api.CompanyId));
                        }
                    }
                }
            };
            dialog.Show();
        }

        async void Subscripe()
        {
            var res = await api.Query<SubscriptionClient>();
            if (res != null && res.Length > 0)
                AddDockItem(TabControls.SubscriptionsPage, res.FirstOrDefault());
        }
        private void CopyLayout()
        {
            UserAPI Uapi = new UserAPI(api);
            CWTextControl cwCopylayoutdialog = new CWTextControl(Uniconta.ClientTools.Localization.lookup("CopyUserLayout"), "LoginId");
            cwCopylayoutdialog.Closed += async delegate
            {
                if (cwCopylayoutdialog.DialogResult == true)
                {
                    var res = await Uapi.CopyUserLayout(cwCopylayoutdialog.InputValue);
                    if (res == ErrorCodes.Succes)
                        BasePage.ClearLayoutCache();
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            cwCopylayoutdialog.Show();

        }
        private void Email_ButtonClicked(object sender)
        {
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }
    }
}
