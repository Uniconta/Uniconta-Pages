using UnicontaClient.Models;
using System;
using System.Linq;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AppPluginPage2 : FormBasePage
    {
        AppPluginClient editrow;
        public override string NameOfControl { get { return TabControls.AppPluginPage2.ToString(); } }
        public override Type TableType { get { return typeof(AppPluginClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (AppPluginClient)value; } }
        public override void OnClosePage(object[] refreshParams)
        {
            object[] argsArray = new object[3];
            argsArray[0] = refreshParams[0];
            argsArray[1] = refreshParams[1];
            argsArray[2] = this.masterUser;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }
        UnicontaBaseEntity masterUser;
        public AppPluginPage2(CrudAPI crudApi, UnicontaBaseEntity master) : base(crudApi, "")
        {
            editrow = CreateNew() as AppPluginClient;
            masterUser = master;
            if (master != null)
                editrow.SetMaster(master);
            InitPage();
            FocusManager.SetFocusedElement(txtName, txtName);
        }
        public AppPluginPage2(UnicontaBaseEntity sourcedata) : base(sourcedata, true)
        {
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            editrow.SetMaster(App.MainPageObj.ActiveApp);
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            cmbControlsName.ItemsSource = UserControlProperties.GetControlNames.OrderBy(x => x).ToList();
            if (LoadedRow == null)
                frmRibbon.DisableButtons("Delete");
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            cmbPluginType.ItemsSource = Enum.GetValues(typeof(PluginControlType));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "Script");
            if (editrow._Script != null)
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Script"));
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AddScriptPage)
            {
                var param = argument as object[];
                if (param != null)
                {
                    editrow._Script = param[1] as byte[];
                    var saveScript = param[0] == null ? false : (bool)param[0];
                    if (saveScript)
                    {
                        ErrorCodes result;
                        if (LoadedRow != null)
                            result = await api.Update(LoadedRow, editrow, LoadedRow);
                        else
                            result = await api.Update(editrow);
                    }
                }
            }
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Script":
                    string className = string.Empty;
                    bool openScriptPage = true;
                    if (editrow._Script == null)
                    {
                        CWScriptClassName cw = new CWScriptClassName();
                        cw.Closing += delegate
                        {
                            if (cw.DialogResult == true)
                            {
                                className = cw.ClassName;
                                editrow._ClassName = className;
                                editrow.NotifyPropertyChanged("ClassName");
                            }
                            else
                                openScriptPage = false;
                        };
                        cw.Show();
                    }
                    if (openScriptPage)
                    {
                        object[] param = new object[] { editrow, className }; ;
                        AddDockItem(TabControls.AddScriptPage, param,
                            string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Script"), editrow.Name),
                            ";component/Assets/img/Add_16x16");
                    }
                    break;
                case "Save":
                    if (string.IsNullOrWhiteSpace(editrow.Dll))
                        editrow.SetMaster(new AppDLL());
                    if (string.IsNullOrEmpty(editrow.ClassName) && editrow.IsControlScript)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("ClassName")), Uniconta.ClientTools.Localization.lookup("Warning"));
                    else
                        frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        AppDLL appDll;
        private void liDll_ButtonClicked(object sender)
        {
            var cwAppPluginsDialog = new CWAppPlugins(api, editrow);
            cwAppPluginsDialog.Closing += delegate
            {
                if (cwAppPluginsDialog.DialogResult == true)
                {
                    appDll = cwAppPluginsDialog.selectedAppDll;
                    if (appDll != null)
                    {
                        editrow.SetMaster(appDll);
                        editrow.Dll = appDll._Name;
                    }
                }
            };
            cwAppPluginsDialog.Show();
        }

        private void cmbPluginType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbPluginType.SelectedIndex == 2)
                liCanDisable.Visibility = Visibility.Visible;
            else
            {
                liCanDisable.Visibility = Visibility.Collapsed;
                editrow.CanDisable = false;
                editrow.NotifyPropertyChanged("CanDisable");
            }
        }
    }
}
