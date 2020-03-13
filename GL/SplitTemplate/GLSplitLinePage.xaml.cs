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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLSplitLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLSplitLineClient); } }
    }
    public partial class GLSplitLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GlSplitLinepage.ToString(); } }     

        public GLSplitLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGLSplitLineGrid);
            dgGLSplitLineGrid.api = api;
            dgGLSplitLineGrid.UpdateMaster(master);
            dgGLSplitLineGrid.BusyIndicator = busyIndicatorFinanceYearGrid;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            
            dgGLSplitLineGrid.RowDoubleClick += dgReportLayout_RowDoubleClick;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        void dgReportLayout_RowDoubleClick()
        {
            var selectedItem = dgGLSplitLineGrid.SelectedItem as GLSplitLineClient;
            if (selectedItem == null)
                return;
            AddDockItem(TabControls.GlSplitLinePage2, selectedItem, Uniconta.ClientTools.Localization.lookup("SplitLine"), "Edit_16x16.png");
        }


        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLSplitLineGrid.SelectedItem as GLSplitLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    object[] ob = new object[2];
                    ob[0] = dgGLSplitLineGrid.masterRecord;
                    ob[1] = api;
                    AddDockItem(TabControls.GlSplitLinePage2, ob, Uniconta.ClientTools.Localization.lookup("SplitLine"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GlSplitLinePage2, selectedItem, Uniconta.ClientTools.Localization.lookup("SplitLine"), "Edit_16x16.png");
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GlSplitLinePage2)
                dgGLSplitLineGrid.UpdateItemSource(argument);
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}