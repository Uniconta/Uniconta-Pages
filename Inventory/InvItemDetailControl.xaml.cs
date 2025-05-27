using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
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
using Uniconta.API.System;
using Uniconta.Common;
using System.Windows;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvItemDetailControl : System.Windows.Controls.UserControl
    {
        CrudAPI _api;
        public CrudAPI api
        {
            get
            {
                return _api;
            }
            set
            {
                _api = value;
                var comp = _api.CompanyEntity;
                if (comp.NumberOfDimensions == 0)
                    usedim.Visibility = Visibility.Collapsed;
                else
                    Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
                setUserFields();
                if (!comp.ItemVariants)
                    useVariants.Visibility = Visibility.Collapsed;
                if (!comp.Project)
                    projectLayGrp.Visibility = Visibility.Collapsed;
                if (!comp.Location || !comp.Warehouse)
                    itemLocation.Visibility = Visibility.Collapsed;
                if (!comp.Warehouse)
                    itemWarehouse.Visibility = Visibility.Collapsed;
                if (!comp.SerialBatchNumbers)
                {
                    itemUseSerialBatch.Visibility = Visibility.Collapsed;
                    itemMandatorySerialBatch.Visibility = Visibility.Collapsed;
                    itemMandatorySerialBatchMarkg.Visibility = Visibility.Collapsed;
                }
                if (!comp.Storage || comp.StorageOnAll)
                    itemUsestorage.Visibility = Visibility.Collapsed;

                if (!comp.InvBOM)
                {
                    itemBOMCostOfLines.Visibility = Visibility.Collapsed;
                    liItemIncludedInBOM.Visibility = Visibility.Collapsed;
                }
                if (!comp.UnitConversion)
                {
                    liPurchaseUnit.Visibility = Visibility.Collapsed;
                    liUSalesUnit.Visibility = Visibility.Collapsed;
                    liUnitGroup.Visibility = Visibility.Collapsed;
                }
                if (!comp.Storage)
                    grpQty.Visibility = Visibility.Collapsed;
                if(comp.HideCostPrice)
                    liCostPrice.Visibility = Visibility.Collapsed;
                if (!comp.SetupSizes)
                    grpSize.Visibility = Visibility.Collapsed;
                if (!comp.InvDuty)
                    liDutyGroup.Visibility = Visibility.Collapsed;
                if (comp._InvGroups)
                {
                    if (!string.IsNullOrEmpty(comp._CategoryGroup))
                    {
                        liCategoryGroup.IsLabelSet = true;
                        liCategoryGroup.Label = comp._CategoryGroup;
                    }
                    if (!string.IsNullOrEmpty(comp._BrandGroup))
                    {
                        liBrandGroup.IsLabelSet = true;
                        liBrandGroup.Label = comp._BrandGroup;
                    }
                }
                else
                    grpBrand.Visibility = Visibility.Collapsed;
            }
        }
        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }

        public InvItemDetailControl()
        {
            InitializeComponent();
            layoutItems.Tag = this;
        }

        public void Refresh(object argument, object dataContext)
        {
            var argumentParams = argument as object[];
            if (argumentParams != null)
            {
                var Operation = Convert.ToInt32(argumentParams[0]);
                if (Operation != 3)
                {
                    layoutItems.DataContext = null;
                    layoutItems.DataContext = dataContext;
                }
            }
        }

        void setUserFields()
        {
            var row = new InvItemClient();
            row.SetMaster(api.CompanyEntity);
            var UserFieldDef = row.UserFieldDef();
            if (UserFieldDef != null)
                UserFieldControl.CreateUserFieldOnPage2(layoutItems, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], this.api, this, true, invDtlLastGroup);
        }

        private void liPhoto_ButtonClicked(object sender)
        {
            var layoutItem = sender as Uniconta.ClientTools.Controls.CorasauLayoutItem;
            var dataContext = layoutItem.DataContext;
            if (dataContext != null && dataContext is InvItemClient)
            {
                var invItem = dataContext as InvItemClient;
                ShowViewer(invItem, invItem._Photo, Uniconta.ClientTools.Localization.lookup("Photo"), ViewerType.Photo);
            }
        }

        private void ShowViewer(InvItemClient invItem, int rowId, string header, ViewerType docViewerType)
        {
            if (rowId == 0)
                return;

            var newDocumentViewer = new DocumentViewerWindow(api, header);
            newDocumentViewer.DocumentViewerType = docViewerType;
            newDocumentViewer.InitViewer(invItem);
            newDocumentViewer.Show();
        }

        private void liURL_ButtonClicked(object sender)
        {
            var layoutItem = sender as Uniconta.ClientTools.Controls.CorasauLayoutItem;
            var dataContext = layoutItem.DataContext;
            if (dataContext != null && dataContext is InvItemClient)
            {
                var invItem = dataContext as InvItemClient;
                ShowViewer(invItem, invItem._URL, Uniconta.ClientTools.Localization.lookup("Url"), ViewerType.Url);
            }
        }

        private void liInvoiceAttachment_ButtonClicked(object sender)
        {
            var layoutItem = sender as Uniconta.ClientTools.Controls.CorasauLayoutItem;
            var dataContext = layoutItem.DataContext;
            if (dataContext != null && dataContext is InvItemClient)
            {
                var invItem = dataContext as InvItemClient;
                ShowViewer(invItem, invItem._URL, Uniconta.ClientTools.Localization.lookup("Attachment"), ViewerType.Attachment);
            }
        }
    }
}
