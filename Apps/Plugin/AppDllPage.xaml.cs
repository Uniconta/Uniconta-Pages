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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.API.Service;
using System.IO;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using System.Reflection;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class AppDLLGridClient : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(AppDLLClient); }
        }
    }

    public partial class AppDLLPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.AppDLLPage; }
        }

        public AppDLLPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public AppDLLPage(UnicontaBaseEntity sourcedata)
            : base(sourcedata)
        {
            InitPage(sourcedata);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgAppDLLGrid.api = api;
            dgAppDLLGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgAppDLLGrid);
            if (master != null)
                dgAppDLLGrid.masterRecords = new List<UnicontaBaseEntity>() { master, Utility.GetCurrentApp() };
            else
                dgAppDLLGrid.UpdateMaster(Utility.GetCurrentApp());
            localMenu.OnItemClicked += localMenu_OnItemClicked;

        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AppDLLPage2)
                dgAppDLLGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAppDLLGrid.SelectedItem as AppDLLClient;

            switch (ActionType)
            {
                case "AddRow":
                    object[] parm = new object[2];
                    parm[0] = LoadedRow;
                    parm[1] = null;
                    AddDockItem(TabControls.AppDLLPage2, api, Uniconta.ClientTools.Localization.lookup("PartnerPlugin"), "Add_16x16");
                    break;

                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AppDLLPage2, selectedItem, string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), selectedItem._Name));
                    break;
                case "UserPluginWhereUsed":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AppPluginPage, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Plugins"), selectedItem._Name));
                    break;
                case "ExportAppDLL":
                    if (selectedItem != null)
                        ExportAppDLL(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ExportAppDLL(AppDLLClient selectedItem)
        {
            var saveFileDailog = UtilDisplay.LoadSaveFileDialog;
            saveFileDailog.DefaultExt = "dll";
            bool? userClickedSave = saveFileDailog.ShowDialog();
            if (userClickedSave != true) return;
            SetBusy();
            if (selectedItem._Data == null)
                await api.Read(selectedItem);
            if (selectedItem._Data == null)
            {
                ClearBusy();
                return;
            }
            try
            {
                File.WriteAllBytes(saveFileDailog.FileName, selectedItem._Data);
                ClearBusy();
            }
            catch (Exception ex)
            {
                ClearBusy();
                UnicontaMessageBox.Show(ex);
            }
        }
    }

}
