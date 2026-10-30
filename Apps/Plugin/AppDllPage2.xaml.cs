using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using System.IO;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class AppDLLPage2 : FormBasePage
    {
        AppDLLClient editrow;

        public override string NameOfControl { get { return TabControls.AppDLLPage2.ToString(); } }

        public override Type TableType { get { return typeof(AppDLLClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (AppDLLClient)value; } }
        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }

        public AppDLLPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            editrow = CreateNew() as AppDLLClient;
            editrow._Updated = DateTime.Now;
            InitPage();
            FocusManager.SetFocusedElement(txtName, txtName);
        }

        public AppDLLPage2(UnicontaBaseEntity sourcedata): base(sourcedata, true)
        {
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            editrow.SetMaster(App.MainPageObj.ActiveApp);
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
           if(LoadedRow== null)
                frmRibbon.DisableButtons( "Delete" );
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveFile":
                    SaveFile();
                    break;
                case "Save":
                    if (!ValidateSave())
                        return;
                    frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void SaveFile()
        {
            var err = await api.Read(editrow);
            var data = editrow._Data;
            if (err == ErrorCodes.Succes && data != null)
            {
                var saveDlg = UtilDisplay.LoadSaveFileDialog;
                saveDlg.FileName = editrow.Name;
                saveDlg.DefaultExt = "dll";
                if (saveDlg.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllBytes(saveDlg.FileName, data);
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex);
                    }
                }
            }
        }
        private bool ValidateSave()
        {
            bool isSucess = false;
            string fileExtension = string.Empty;
            byte[] fileBytes;
            if (firstDll)
            {
                fileExtension = fileBrowser1.FileExtension;
                fileBytes = fileBrowser1.FileBytes;
            }
            else
            {
                fileExtension = fileBrowser2.FileExtension;
                fileBytes = fileBrowser2.FileBytes;
            }

            if (LoadedRow == null)
            {
                if (fileBytes != null)
                {
                    editrow.DllData = fileBytes;
                    isSucess = true;
                }
            }
            else
            {
                if (fileBytes != null)
                {
                    var org = (AppDLLClient)LoadedRow;
                    if (!StreamingManager.equal(org._Data, fileBytes))
                        editrow.DllData = fileBytes;
                    else
                        editrow._Data = null;  // if DLL is not changed, then do not upload it.
                    org._Data = null;  // this way we do not upload a large DLL, with we will replace.
                }
                isSucess = true;
            }
            return isSucess;
        }

        bool firstDll = false;
        private void fileBrowser1_FileSelected()
        {
           string browseUrl = fileBrowser1.FilePath;
            if (!string.IsNullOrEmpty(browseUrl))
            {
                grpDllB.IsEnabled = false;
                editrow._LoadedDLL = 1;
                firstDll = true;
            }
        }

        private void fileBrowser2_FileSelected()
        {
            string browseUrl = fileBrowser2.FilePath;
            if (!string.IsNullOrEmpty(browseUrl))
            {
                grpDllA.IsEnabled = false;
                editrow._LoadedDLL = 2;
                firstDll = false;
            }
        }
    }
}
