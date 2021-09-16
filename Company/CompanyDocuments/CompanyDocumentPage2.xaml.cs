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
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Utilities;
using Uniconta.API.System;
using System.IO;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CompanyDocumentPage2 : FormBasePage
    {
        CompanyDocumentClient companyDocumentClientRow;
        List<string> docsAdded;
        public CompanyDocumentPage2(UnicontaBaseEntity sourcedata, List<string> existingDocs)
            : base(sourcedata, true)
        {
            InitializeComponent();
            docsAdded = existingDocs;
            InitPage(api);
        }

        public CompanyDocumentPage2(CrudAPI crudApi, List<string> existingDocs)
            : base(crudApi, "")
        {
            InitializeComponent();
            docsAdded = existingDocs;
            InitPage(crudApi);
        }

        private void InitPage(CrudAPI crudApi)
        {
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                companyDocumentClientRow = CreateNew() as CompanyDocumentClient;
                cmbDocumentUse.ItemsSource = GetDocumentUsage(AppEnums.CompanyDoc.Values);
                cmbDocumentUse.SelectedIndex = 0;
            }
            else
            {
                cmbDocumentUse.ItemsSource = AppEnums.CompanyDoc.Values;
                cmbDocumentUse.SelectedItem = companyDocumentClientRow.DocumentUseFor;
            }
            layoutItems.DataContext = companyDocumentClientRow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

#if !SILVERLIGHT
            browseControl.CompressVisibility = Visibility.Visible;
            string imgfilter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;...";
#else
            string imgfilter = "Image files (*.bmp,*.jpg) |*.bmp; *.jpg";
#endif
            browseControl.Filter = imgfilter;

        }

        private List<string> GetDocumentUsage(string[] doucmentsUsage)
        {
            List<string> updatedList = doucmentsUsage.ToList();
            if (docsAdded != null)
                foreach (var item in docsAdded)
                    updatedList.Remove(item);
            return updatedList;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (!ValidateSave())
                        return;
                    SaveDoc();
                    break;
                case "Delete":
                    DeleteDoc();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void SaveDoc()
        {
            closePageOnSave = false;
            var recordSaved = await saveForm();

            if (recordSaved)
            {
                var companyClient = api.CompanyEntity as CompanyClient;

                if (companyDocumentClientRow.UseFor == CompanyDocumentUse.TopBarLogo)
                {
                    companyClient.TopBarLogo = null;
                    globalEvents?.NotifyCompanyLogoUpdate(this, new CompanyEventArgs(companyClient));
                }
                else if (companyDocumentClientRow.UseFor == CompanyDocumentUse.CompanyLogo)
                    companyClient.Logo = null;
            }
            CloseDockItem();
        }
        async void DeleteDoc()
        {
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                await DeleteAsync();

            var companyClient = api.CompanyEntity as CompanyClient;
            if (companyDocumentClientRow.UseFor == CompanyDocumentUse.TopBarLogo)
            {
                companyClient.TopBarLogo = null;
                globalEvents?.NotifyCompanyLogoUpdate(this, new CompanyEventArgs(companyClient));
            }
            else if (companyDocumentClientRow.UseFor == CompanyDocumentUse.CompanyLogo)
                companyClient.Logo = null;
        }
        private bool ValidateSave()
        {
            bool isSucess = false;
            string fileExtension = browseControl.FileExtension;
            byte[] fileBytes = browseControl.FileBytes;
            if (fileBytes != null)
            {
#if !SILVERLIGHT
                try
                {
                    var img = System.Drawing.Image.FromStream(new MemoryStream(fileBytes));
                }
                catch (Exception)
                {
                    fileBytes = null;
                }
#else
                string[] validExtensions = {"jpg","bmp","jpeg","png"}; 
                if(!string.IsNullOrEmpty(fileExtension) && !validExtensions.Contains(fileExtension))
                  fileBytes = null;
#endif
            }
            if (cmbDocumentUse.SelectedItemValue == null)
            {
                UnicontaMessageBox.Show(string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CompanyDocumentUse"), Uniconta.ClientTools.Localization.lookup("FieldHasinvalidValue")), Uniconta.ClientTools.Localization.lookup("Error"));
                isSucess = false;
                return isSucess;
            }
            //Adding  a new Record
            if (LoadedRow == null)
            {
                if (fileBytes != null)
                {
                    companyDocumentClientRow.DocumentData = fileBytes;
                    companyDocumentClientRow.DocumentUseFor = Convert.ToString(cmbDocumentUse.SelectedItemValue);
                    companyDocumentClientRow.DocumentType = DocumentConvert.GetDocumentType(fileExtension);
                    isSucess = true;
                }
            }
            else //editing a Record
            {
                if (fileBytes != null)
                {
                    companyDocumentClientRow.DocumentData = fileBytes;
                    companyDocumentClientRow.DocumentType = DocumentConvert.GetDocumentType(fileExtension);
                }
                isSucess = true;
            }
            if (companyDocumentClientRow.UseFor == CompanyDocumentUse.TopBarLogo)
            {
                if (fileBytes != null && fileBytes.Length > 100 * 1024)
                {
                    isSucess = false;
                    UnicontaMessageBox.Show(string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("MaxFileSizeLimit"), "100KB"),Uniconta.ClientTools.Localization.lookup("Error"));
                }
            }
            return isSucess;
        }

        public override Type TableType { get { return typeof(CompanyDocumentClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return companyDocumentClientRow; } set { companyDocumentClientRow = (CompanyDocumentClient)value; } }
        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }
        public override string NameOfControl
        {
            get { return TabControls.CompanyDocumentPage2.ToString(); }
        }

        private void cmbDocumentUse_SelectedIndexChanged(object sender, RoutedEventArgs e)
      {
            /* check if key does not exists*/
            if (cmbDocumentUse.SelectedIndex == -1)
                return;
            if (AppEnums.CompanyDoc.Values.Count() >= cmbDocumentUse.SelectedIndex && Uniconta.ClientTools.Localization.lookup(AppEnums.CompanyDoc.Label(cmbDocumentUse.SelectedIndex)) == companyDocumentClientRow.DocSuggestion)
                suggestionControl.Visibility = Visibility.Visible;
            else
                suggestionControl.Visibility = Visibility.Collapsed;
        }
    }
}
