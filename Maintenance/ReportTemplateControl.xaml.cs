using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{ 
    public class GLReportTemplateGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(GLReportTemplateClient); }
        }
    }
    public partial class GLReportTemplatePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.GLReportTemplate.ToString(); }
        }
        public GLReportTemplatePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public GLReportTemplatePage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgGLReportTemplate;
            SetRibbonControl(localMenu, dgGLReportTemplate);
            dgGLReportTemplate.api = api;
            dgGLReportTemplate.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLReportTemplate.RowDoubleClick += dgGLReportTemplate_RowDoubleClick;
        }
        void dgGLReportTemplate_RowDoubleClick()
        {
            var selectedItem = dgGLReportTemplate.SelectedItem as GLReportTemplateClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GLReportLine, selectedItem, string.Format("{0}: {1}", Localization.lookup("ReportLines"), selectedItem._Name));
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLReportTemplatePage2)
                dgGLReportTemplate.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLReportTemplate.SelectedItem as GLReportTemplateClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLReportTemplatePage2, api, Uniconta.ClientTools.Localization.lookup("CompanyAccountTemplate"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CompanyAccountTemplate"), selectedItem._Name);
                    AddDockItem(TabControls.GLReportTemplatePage2, selectedItem, header, "Edit_16x16.png");
                    break;
                case "ReportLines":
                    dgGLReportTemplate_RowDoubleClick();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
