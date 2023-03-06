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
using Uniconta.Common;
using UnicontaClient.Utilities;
using System.Threading.Tasks;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyDocumentsGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(CompanyDocumentClient); }
        }
    }

    public partial class CompanyDocumentPage : GridBasePage
    {
        public CompanyDocumentPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public CompanyDocumentPage(UnicontaBaseEntity baseEntity)
            : base(null)
        {
            InitPage();
            dgCompanyDocsGrid.SetSource(new UnicontaBaseEntity[] { baseEntity });
        }

        private void InitPage()
        {
            InitializeComponent();
            this.DataContext = this;
            dgCompanyDocsGrid.api = api;
            dgCompanyDocsGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCompanyDocsGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            List<string> documents = PrepareExistingDocumentList(dgCompanyDocsGrid.VisibleRowCount);
            var selectedItem = dgCompanyDocsGrid.SelectedItem as CompanyDocumentClient;

            switch (ActionType)
            {
                case "AddRow":
                    object[] paramAdd = new object[2];
                    paramAdd[0] = api;
                    paramAdd[1] = documents;
                    AddDockItem(TabControls.CompanyDocumentPage2, paramAdd, Uniconta.ClientTools.Localization.lookup("CompanyImages"), "Add_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] paramEdit = new object[2];
                    paramEdit[0] = selectedItem;
                    paramEdit[1] = documents;
                    AddDockItem(TabControls.CompanyDocumentPage2, paramEdit, Uniconta.ClientTools.Localization.lookup("CompanyImages"), "Edit_16x16.png");
                    break;

                case "ViewDownloadRow":
                    if (selectedItem != null)
                        ShowContent(selectedItem);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private List<string> PrepareExistingDocumentList(int rows)
        {
            List<string> docs = new List<string>();
            for (int i = 0; i < rows; i++)
            {
                var item = dgCompanyDocsGrid.GetRow(i) as CompanyDocumentClient;
                if (item.UseFor == CompanyDocumentUse.TopBarLogo || item.UseFor== CompanyDocumentUse.Icon || item.UseFor == CompanyDocumentUse.CompanyLogo)
                    docs.Add(item.DocumentUseFor);
            }
            return docs;
        }

        async void ShowContent(CompanyDocumentClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            await api.Read(selectedItem);
            CWDocumentViewer viewer = new CWDocumentViewer(selectedItem.DocumentData, selectedItem.DocumentType);
            viewer.Show();
            busyIndicator.IsBusy = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CompanyDocumentPage2)
                dgCompanyDocsGrid.UpdateItemSource(argument);
        }

        public override string NameOfControl
        {
            get { return TabControls.CompanyDocumentPage.ToString(); }
        }
    }
}
