using UnicontaClient.Models;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectCategoryChargeGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectCategoryChargeClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class ProjectCategoryChargePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectCategoryChargePage; } }
        public ProjectCategoryChargePage(UnicontaBaseEntity master) : base(master)
        {
            InitializePage(master as ProjectCategoryClient);
        }

        private void InitializePage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectCategoryChargeGrid);
            dgProjectCategoryChargeGrid.api = api;
            dgProjectCategoryChargeGrid.BusyIndicator = busyIndicator;
            if (master != null)
                dgProjectCategoryChargeGrid.UpdateMaster(master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectCategoryChargeGrid.AddRow();
                    var selectedItem = dgProjectCategoryChargeGrid.SelectedItem as ProjectCategoryChargeClient;
                    if (selectedItem != null && selectedItem._PrCategory != null)
                    {
                        var Comp = api.CompanyEntity;
                        var cache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory));
                        if (cache != null)
                        {
                            var rec = (Uniconta.DataModel.PrCategory)cache.Get(selectedItem._PrCategory);
                            if (rec != null && rec._CatType != Uniconta.DataModel.CategoryType.Labour)
                            {
                                selectedItem._ChargeType = Uniconta.DataModel.PrCategoryChargeType.PctOnSales;
                                selectedItem.RefreshChartType();
                            }
                        }
                    }
                    break;
                case "DeleteRow":
                    dgProjectCategoryChargeGrid.DeleteRow();
                    break;

                case "SaveGrid":
                    dgProjectCategoryChargeGrid.SaveData();
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
