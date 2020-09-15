using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectCategoryGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectCategoryClient); } }
        public override IComparer GridSorting { get { return new ProjectCategorySort(); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectCategoryPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectCategoryPage; } }

        public ProjectCategoryPage(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        public ProjectCategoryPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectCategoryPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectCategorygrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        
        private void SetHeader()
        {
            var syncMaster = dgProjectCategorygrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectCategory"), syncMaster._Name);
            SetHeader(header);
        }
        void InitPage(UnicontaBaseEntity master)
        {
            SetRibbonControl(localMenu, dgProjectCategorygrid);
            dgProjectCategorygrid.api = api;
            dgProjectCategorygrid.BusyIndicator = busyIndicator;
            dgProjectCategorygrid.UpdateMaster(master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadType(typeof(Uniconta.DataModel.PrCategory));
            if (master == null)
                Project.Visible = true;
            else
            {
                var proj = (master as Uniconta.DataModel.Project);
                if (proj != null)
                    proj.Categories = null;
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectCategorygrid.AddRow();
                    break;

                case "Delete":
                    dgProjectCategorygrid.DeleteRow();
                    break;

                case "SaveGrid":
                    dgProjectCategorygrid.SaveData();
                    break;

                case "SetCharge":
                    dgProjectCategorygrid.SaveData();
                    var selectedItem = dgProjectCategorygrid.SelectedItem as ProjectCategoryClient;
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectCategoryChargePage, selectedItem, string.Format("{0}: {1},{2}", Uniconta.ClientTools.Localization.lookup("CategoryCharge"),
                                selectedItem._Project, selectedItem._PrCategory));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
