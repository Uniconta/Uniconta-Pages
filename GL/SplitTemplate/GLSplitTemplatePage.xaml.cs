using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
    public class SplittemplateGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLSplitTemplateClient); } }
    }
    public partial class GLSplitTemplatePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLSplitTemplatePage.ToString(); } }
        protected override bool IsLayoutSaveRequired() { return false; }
        public GLSplitTemplatePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public GLSplitTemplatePage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgSplittemplateGrid);
            dgSplittemplateGrid.api = api;
            dgSplittemplateGrid.BusyIndicator = busyIndicatorFinanceYearGrid;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            
            dgSplittemplateGrid.RowDoubleClick += dgReportLayout_RowDoubleClick;
        }
        void dgReportLayout_RowDoubleClick()
        {
            var selectedItem = dgSplittemplateGrid.SelectedItem as GLSplitTemplateClient;
            if (selectedItem == null)
                return;
            AddDockItem(TabControls.GLSplitTemplatePage2, selectedItem, Localization.lookup("SplitTemplate"), "Edit_16x16.png");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgSplittemplateGrid.SelectedItem as GLSplitTemplateClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLSplitTemplatePage2, api, Uniconta.ClientTools.Localization.lookup("SplitTemplate"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GLSplitTemplatePage2, selectedItem, Uniconta.ClientTools.Localization.lookup("SplitTemplate"), "Edit_16x16.png");
                    break;
                case "SplitLine":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GlSplitLinepage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("SplitLine"), selectedItem.Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLSplitTemplatePage2)
                dgSplittemplateGrid.UpdateItemSource(argument);
        }
    }
}
