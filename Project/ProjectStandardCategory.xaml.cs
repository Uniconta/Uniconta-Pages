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
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System.Collections;
using UnicontaClient.Models;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PrStandardCategoryGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrStandardCategoryClient); } }
        public override IComparer GridSorting { get { return new PrStandardCategorySort(); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectStandardCategory : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PrStandardCategoryPage; } }

        public ProjectStandardCategory(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        public ProjectStandardCategory(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgStandardCategoryClient;
            SetRibbonControl(localMenu, dgStandardCategoryClient);
            dgStandardCategoryClient.api = api;
            dgStandardCategoryClient.BusyIndicator = busyIndicator;
            dgStandardCategoryClient.UpdateMaster(master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadType(typeof(Uniconta.DataModel.PrCategory));
            if (master == null)
                PrStandard.Visible = true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgStandardCategoryClient.AddRow();
                    break;

                case "Delete":
                    dgStandardCategoryClient.DeleteRow();
                    break;

                case "SaveGrid":
                    dgStandardCategoryClient.SaveData();
                    break;

                case "SetCharge":
                    dgStandardCategoryClient.SaveData();
                    var selectedItem = dgStandardCategoryClient.SelectedItem as PrStandardCategoryClient;
                    if (selectedItem != null)
                        AddDockItem(TabControls.PrStandardCategoryChargePage, selectedItem, string.Format("{0}:{1}, {2}", Uniconta.ClientTools.Localization.lookup("CategoryCharge"),
                                selectedItem._PrStandard, selectedItem._PrCategory));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
