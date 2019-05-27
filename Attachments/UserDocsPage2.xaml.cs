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
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.API.System;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using System.IO;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserDocsPage2 : FormBasePage
    {
        UserDocsClient userDocsClientRow;
        bool isFieldsAvailableForEdit;
        public override Type TableType { get { return typeof(UserDocsClient); } }
        public override string NameOfControl { get { return TabControls.UserDocsPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return userDocsClientRow; } set { userDocsClientRow = (UserDocsClient)value; } }
        public UserDocsPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            isFieldsAvailableForEdit = isEdit;
            InitPage(api);
        }

        public UserDocsPage2(UnicontaBaseEntity sourcedata, bool isEdit, CrudAPI crudApi)
           : base(crudApi, "")
        {
            InitializeComponent();
            isFieldsAvailableForEdit = isEdit;
            ModifiedRow = sourcedata;
            InitPage(crudApi);
        }

        public UserDocsPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }
        private void InitPage(CrudAPI api)
        {
            leGroup.api = api;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            layoutItems.DataContext = userDocsClientRow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            var TableId = userDocsClientRow._TableId;
            if (TableId != 71 && TableId != 72 && TableId != 73 && TableId != 77 && TableId != 78 && TableId != 79) // Sales Order, purchase order, Offer, Invoices
                groupInclude.Visibility = Visibility.Collapsed;
            else if (TableId != 72) /*Purchase Order */
                layoutRequisition.Visibility = Visibility.Collapsed;

            if (LoadedRow == null)
            {
                api.AllowBackgroundCrud = false;
                frmRibbon.DisableButtons("Delete");
            }

#if !SILVERLIGHT
            browseControl.CompressVisibility = Visibility.Visible;
#endif
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                if (!ValidateSave())
                {
                    busyIndicator.IsBusy = false;
                    Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoFilesSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                }
                else
                {
#if !SILVERLIGHT
                    if (!string.IsNullOrEmpty(browseControl?.FileName) && !(bool)chkIncludeOnlyReference.IsChecked)
#else
                    if (!string.IsNullOrEmpty(browseControl?.FileName))
#endif
                    {
                        int indexOfExtention = browseControl.FileName.IndexOf('.');
                        var nameOfFile = indexOfExtention > 0 ? browseControl.FileName.Substring(0, indexOfExtention) : browseControl.FileName;
                        userDocsClientRow.DocumentType = DocumentConvert.GetDocumentType(browseControl.FileExtension);
                        userDocsClientRow.Text = string.IsNullOrWhiteSpace(txedUserDocNotes.Text) ? nameOfFile : txedUserDocNotes.Text;
                    }
                    else if (!string.IsNullOrWhiteSpace(userDocsClientRow._Url))
                    {
                        var url = userDocsClientRow._Url;
                        if (url.EndsWith("/"))
                            url = url.Substring(0, url.Length - 1);
                        int indexOfExtention = url.LastIndexOf('.');
                        int indexOfpath = url.LastIndexOf('/') + 1;
                        if (indexOfpath == -1)
                            indexOfpath = url.LastIndexOf('\\') + 1;
                        var nameOfFile = indexOfpath > 0 ? url.Substring(indexOfpath, indexOfExtention - indexOfpath) : url;
                        userDocsClientRow.DocumentType = DocumentConvert.GetDocumentType(url.Substring(indexOfExtention, url.Length - indexOfExtention));
                        userDocsClientRow.Text = string.IsNullOrWhiteSpace(txedUserDocNotes.Text) ? nameOfFile : txedUserDocNotes.Text;
                    }
                    else
                        userDocsClientRow.Text = txedUserDocNotes.Text;
                    saveForm();
                }
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        private bool ValidateSave()
        {
            bool isSucess = false;
            byte[] fileBytes = browseControl.FileBytes;
            if (fileBytes != null && string.IsNullOrEmpty(userDocsClientRow._Url))
            {
                userDocsClientRow.UserDocument = fileBytes;
                userDocsClientRow._NoCompression = true; // we are already compressed on the client
                isSucess = true;
            }
            else if (!string.IsNullOrWhiteSpace(userDocsClientRow._Url))
                isSucess = true;
            else if (userDocsClientRow.UserDocument != null)
                isSucess = true;
            else if (isFieldsAvailableForEdit)
                isSucess = true;
            return isSucess;
        }

        FileextensionsTypes GetUrlFileExtension()
        {
            FileextensionsTypes extension;
            var url = userDocsClientRow._Url;
            int indexOfExtention = url.LastIndexOf('.');
            int indexOfpath = url.LastIndexOf('/') + 1;
            if (indexOfpath == -1)
                indexOfpath = url.LastIndexOf('\\') + 1;
            var nameOfFile = indexOfpath > 0 ? url.Substring(indexOfpath, indexOfExtention - indexOfpath) : url;
            extension = DocumentConvert.GetDocumentType(url.Substring(indexOfExtention, url.Length - indexOfExtention));
            return extension;
        }

        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
#if !SILVERLIGHT
        string browseUrl;
        private void browseControl_FileSelected()
        {
            browseUrl = browseControl.FilePath;
            if (chkIncludeOnlyReference.IsChecked == true)
                userDocsClientRow.Url = browseUrl;
        }

        private void chkIncludeOnlyReference_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            browseControl.CheckMaxSize = !chkIncludeOnlyReference.IsChecked.Value;
            if (chkIncludeOnlyReference.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(browseUrl))
                    userDocsClientRow.Url = browseUrl;
            }
            else
                userDocsClientRow.Url = null;
        }
#endif
    }
}
