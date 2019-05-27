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
    public class PrStandardCategoryChargeGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrStandardCategoryChargeClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectStandardCategoryChargePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PrStandardCategoryChargePage; } }

        public ProjectStandardCategoryChargePage(UnicontaBaseEntity sourceMaster) : base(sourceMaster)
        {
            InitializedPage(sourceMaster as PrStandardCategoryClient);
        }

        private void InitializedPage(PrStandardCategoryClient master = null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgStandardCategoryChargeGrid);
            dgStandardCategoryChargeGrid.api = api;
            dgStandardCategoryChargeGrid.BusyIndicator = busyIndicator;
            if (master != null)
                dgStandardCategoryChargeGrid.UpdateMaster(master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
       
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
                case "AddRow":
                    dgStandardCategoryChargeGrid.AddRow();
                    var selectedItem = dgStandardCategoryChargeGrid.SelectedItem as PrStandardCategoryChargeClient;
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
                    dgStandardCategoryChargeGrid.DeleteRow();
                    break;

                case "SaveGrid":
                    dgStandardCategoryChargeGrid.SaveData();
                    break;

                default:gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
