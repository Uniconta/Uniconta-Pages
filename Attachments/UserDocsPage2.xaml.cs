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
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserDocsPage2 : FormBasePage
    {
        UserDocsClient userDocsClientRow;
        bool isFieldsAvailableForEdit;
        bool isFileExtManualSet;
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
            layoutControl = layoutItems;
            layoutItems.DataContext = userDocsClientRow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            var TableId = userDocsClientRow._TableId;
            if (TableId != DebtorOrder.CLASSID && TableId != CreditorOrder.CLASSID && TableId != DebtorOffer.CLASSID &&
                TableId != DebtorInvoice.CLASSID && TableId != Uniconta.DataModel.CreditorInvoice.CLASSID && 
                TableId != ProjectOrder.CLASSID && TableId != ProjectInvoiceProposalClient.CLASSID && TableId != ProductionOrderClient.CLASSID &&
                TableId != CompanySMTP.CLASSID)
                groupInclude.Visibility = Visibility.Collapsed;
            else if (TableId != CreditorOrderClient.CLASSID) /*Purchase Order */
                layoutRequisition.Visibility = Visibility.Collapsed;
            
            if (TableId == ProjectInvoiceProposalClient.CLASSID) /* Project Invoice Proposal*/
            {
                layoutOffer.Visibility = Visibility.Collapsed;
                layoutConfirmation.Visibility = Visibility.Collapsed;
                layoutPacknote.Visibility = Visibility.Collapsed;
            }

            if (TableId == ProductionOrderClient.CLASSID) /*Production Order*/
            {
                layoutInvoice.Visibility = Visibility.Collapsed;
                layoutOffer.Visibility = Visibility.Collapsed;
                layoutConfirmation.Visibility = Visibility.Collapsed;
                layoutPacknote.Visibility = Visibility.Collapsed;
            }

            if (LoadedRow == null)
            {
                var RowId = userDocsClientRow._TableRowId;
                SetTemplateDefault(userDocsClientRow);
                userDocsClientRow._TableId = TableId;
                userDocsClientRow._TableRowId = RowId;
                api.AllowBackgroundCrud = false;
                frmRibbon.DisableButtons("Delete");
            }

            browseControl.CompressVisibility = Visibility.Visible;
            liDocumentType.Visibility = isFieldsAvailableForEdit ? Visibility.Visible : Visibility.Collapsed;
            txtUrl.LostFocus += txtUrl_LostFocus;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                if (!ValidateSave())
                {
                    Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoFilesSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                }
                else
                {
                    if (!string.IsNullOrEmpty(browseControl?.FileName) && !(bool)chkIncludeOnlyReference.IsChecked)
                    {
                        int indexOfExtention = browseControl.FileName.IndexOf('.');
                        userDocsClientRow.DocumentType = DocumentConvert.GetDocumentType(browseControl.FileExtension);
                        var nameOfFile = indexOfExtention > 0 && userDocsClientRow.DocumentType != FileextensionsTypes.UNK ? browseControl.FileName.Substring(0, indexOfExtention) : browseControl.FileName;
                        userDocsClientRow.Text = string.IsNullOrWhiteSpace(txedUserDocNotes.Text) ? nameOfFile : txedUserDocNotes.Text;
                        userDocsClientRow._Url = null;
                    }
                    else if (!string.IsNullOrWhiteSpace(userDocsClientRow._Url))
                    {
                        string fileName = txedUserDocNotes.Text;
                        FileextensionsTypes fileExt = userDocsClientRow._DocumentType;

                        if (!isFileExtManualSet)
                        {
                            var url = userDocsClientRow._Url;
                            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("www", StringComparison.OrdinalIgnoreCase))
                            {
                                int idxExtension = url.LastIndexOf('.');
                                var ext = DocumentConvert.GetDocumentType(url.Substring(idxExtension, url.Length - idxExtension));
                                fileExt = ext != FileextensionsTypes.UNK ? ext : FileextensionsTypes.WWW;
                            }
                            else
                            if (!Utility.TryParseUrl(url, isFieldsAvailableForEdit, ref fileName, ref fileExt)) return;
                        }
                        /* only updating if different */
                        if (userDocsClientRow.DocumentType != fileExt)
                            userDocsClientRow.DocumentType = fileExt;

                        userDocsClientRow.Text = fileName;
                    }
                    else
                        userDocsClientRow.Text = txedUserDocNotes.Text;
                    txtUrl.LostFocus -= txtUrl_LostFocus;
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
        string browseUrl;
        private void browseControl_FileSelected()
        {
            browseUrl = browseControl.FilePath;
            if (chkIncludeOnlyReference.IsChecked == true && !string.IsNullOrEmpty(browseUrl))
            {
                userDocsClientRow.Url = browseUrl;
                string fileName = txedUserDocNotes.Text;
                FileextensionsTypes fileExt = userDocsClientRow._DocumentType;

                if (Utility.TryParseUrl(browseUrl, isFieldsAvailableForEdit, ref fileName, ref fileExt))
                    userDocsClientRow.DocumentType = fileExt;
            }
        }

        private void chkIncludeOnlyReference_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            browseControl.CheckMaxSize = !chkIncludeOnlyReference.IsChecked.GetValueOrDefault();
            if (chkIncludeOnlyReference.IsChecked == true)
                browseControl_FileSelected();
            else
                userDocsClientRow.Url = null;
        }

        private void txtUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(userDocsClientRow._Url))
                liDocumentType.Visibility = isFieldsAvailableForEdit ? Visibility.Visible : Visibility.Collapsed;
            else
            {
                liDocumentType.Visibility = Visibility.Visible;
                userDocsClientRow.DocumentType = FileextensionsTypes.WWW;
            }
        }

        private void cmbDocType_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            FileextensionsTypes updatedFileExt;
            if (e.OldValue != e.NewValue && Enum.TryParse(Convert.ToString(e.NewValue), out updatedFileExt) && userDocsClientRow.DocumentType != updatedFileExt)
            {
                string comments = userDocsClientRow._Text;
                if (Utility.TryParseUrl(txtUrl.Text, isFieldsAvailableForEdit, ref comments, ref updatedFileExt))
                {
                    userDocsClientRow.DocumentType = updatedFileExt;
                    userDocsClientRow._Text = comments;
                    isFileExtManualSet = true;
                }
            }
        }
    }
}
