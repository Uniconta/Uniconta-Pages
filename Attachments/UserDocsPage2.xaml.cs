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
using Uniconta.ClientTools.Controls;

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
            if (TableId != 71 && TableId != 72 && TableId != 73 && TableId != 77 && TableId != 78 && TableId != 79 && TableId != 205) // Sales Order, purchase order, Offer, Invoices, Production Order
                groupInclude.Visibility = Visibility.Collapsed;
            else if (TableId != 72) /*Purchase Order */
                layoutRequisition.Visibility = Visibility.Collapsed;

            if (TableId == 205) /*Production Order*/
            {
                layoutInvoice.Visibility = Visibility.Collapsed;
                layoutOffer.Visibility = Visibility.Collapsed;
                layoutConfirmation.Visibility = Visibility.Collapsed;
                layoutPacknote.Visibility = Visibility.Collapsed;
            }

            if (LoadedRow == null)
            {
                SetTemplateDefault(userDocsClientRow);
                api.AllowBackgroundCrud = false;
                frmRibbon.DisableButtons("Delete");
            }

#if !SILVERLIGHT
            browseControl.CompressVisibility = Visibility.Visible;
            if (isFieldsAvailableForEdit)
                liDocumentType.Visibility = Visibility.Visible;
            else
                liDocumentType.Visibility = Visibility.Collapsed;
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
                        userDocsClientRow._Url = null;
                    }
                    else if (!string.IsNullOrWhiteSpace(userDocsClientRow._Url))
                    {
                        var url = userDocsClientRow._Url;
                        string fileName = txedUserDocNotes.Text;
                        FileextensionsTypes fileExt = isFieldsAvailableForEdit ? userDocsClientRow._DocumentType : FileextensionsTypes.UNK;
#if !SILVERLIGHT
                        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("www", StringComparison.OrdinalIgnoreCase))
                        {
                            int idxExtension = url.LastIndexOf('.');
                            if (fileExt == FileextensionsTypes.UNK)
                            {
                                var ext = DocumentConvert.GetDocumentType(url.Substring(idxExtension, url.Length - idxExtension));
                                fileExt = ext != FileextensionsTypes.UNK ? ext : FileextensionsTypes.WWW;
                            }

                        }
                        else
#endif
                            if (!TryParseUrl(url, ref fileName, ref fileExt)) return;

                        userDocsClientRow.DocumentType = fileExt;
                        userDocsClientRow.Text = fileName;
                    }
                    else
                        userDocsClientRow.Text = txedUserDocNotes.Text;
                    saveForm();
                }
            }
            else
            {
                if (ActionType == "Delete")
                    api.CompanyEntity.AttachmentCacheDelete(userDocsClientRow);

                frmRibbon_BaseActions(ActionType);
            }
        }

        private bool TryParseUrl(string url, ref string fielName, ref FileextensionsTypes fileExtension)
        {
            try
            {
                if (url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);
                int indexOfExtention = url.LastIndexOf('.');
                int indexOfpath = url.LastIndexOf('/') + 1;
                if (indexOfpath == 0)
                    indexOfpath = url.LastIndexOf('\\') + 1;
                if (indexOfpath > 0 && indexOfExtention > indexOfpath)
                {
                    var nameOfFile = indexOfpath > 0 ? url.Substring(indexOfpath, indexOfExtention - indexOfpath) : url;
                    var ext = DocumentConvert.GetDocumentType(url.Substring(indexOfExtention, url.Length - indexOfExtention));
                    if (!isFieldsAvailableForEdit)
                        fileExtension = ext;
                    else
                    {
                        if (fileExtension == FileextensionsTypes.UNK || fileExtension != ext)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Warning"));
                            return false;
                        }
                    }
                    fielName = string.IsNullOrWhiteSpace(fielName) ? nameOfFile : fielName;

                }
                return true;
            }
            catch { return false; }
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


        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
#if !SILVERLIGHT
        string browseUrl;
        private void browseControl_FileSelected()
        {
            browseUrl = browseControl.FilePath;
            if (chkIncludeOnlyReference.IsChecked == true && !string.IsNullOrEmpty(browseUrl))
            {
                userDocsClientRow.Url = browseUrl;
                string fileName = txedUserDocNotes.Text;
                FileextensionsTypes fileExt = userDocsClientRow._DocumentType;

                if (TryParseUrl(browseUrl, ref fileName, ref fileExt))
                    userDocsClientRow.DocumentType = fileExt;
            }
        }

        private void chkIncludeOnlyReference_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            browseControl.CheckMaxSize = !chkIncludeOnlyReference.IsChecked.Value;
            if (chkIncludeOnlyReference.IsChecked == true)
                browseControl_FileSelected();
            else
                userDocsClientRow.Url = null;
        }
#endif
    }
}
