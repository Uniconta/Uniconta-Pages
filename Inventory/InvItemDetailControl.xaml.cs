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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvItemDetailControl : UserControl
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
                if (_api.CompanyEntity.NumberOfDimensions == 0)
                    usedim.Visibility = Visibility.Collapsed;
                else
                    Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
                setUserFields();
                if (!api.CompanyEntity.ItemVariants)
                    useVariants.Visibility = Visibility.Collapsed; 
                if (!api.CompanyEntity.Project)
                    projectLayGrp.Visibility = Visibility.Collapsed;
                if (!api.CompanyEntity.Location || !api.CompanyEntity.Warehouse)
                    itemLocation.Visibility = Visibility.Collapsed;
                if (!api.CompanyEntity.Warehouse)
                    itemWarehouse.Visibility = Visibility.Collapsed;
                if (!api.CompanyEntity.SerialBatchNumbers)
                {
                    itemUseSerialBatch.Visibility = Visibility.Collapsed;
                    itemMandatorySerialBatch.Visibility = Visibility.Collapsed;
                    itemMandatorySerialBatchMarkg.Visibility = Visibility.Collapsed;
                }
                if (!api.CompanyEntity.Storage || api.CompanyEntity.StorageOnAll)
                    itemUsestorage.Visibility = Visibility.Collapsed;

                if(!api.CompanyEntity.InvBOM)
                {
                    itemBOMCostOfLines.Visibility = Visibility.Collapsed;
                    liItemIncludedInBOM.Visibility = Visibility.Collapsed;
                }
                if (!api.CompanyEntity.UnitConversion)
                {
                    liPurchaseUnit.Visibility = Visibility.Collapsed;
                    liUSalesUnit.Visibility = Visibility.Collapsed;
                    liUnitGroup.Visibility = Visibility.Collapsed;
                }
                if (!api.CompanyEntity.Storage)
                    grpQty.Visibility = Visibility.Collapsed;
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
                UserFieldControl.CreateUserFieldOnPage2(layoutItems, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], this.api, this, true, invDtlLastGroup );
        }
    }
}
