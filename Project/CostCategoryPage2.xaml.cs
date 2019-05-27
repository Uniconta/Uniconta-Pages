using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CostCategoryPage2 : FormBasePage
    {
        ProjectCostCategoryClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.CostCategoryPage2.ToString(); } }
        public override Type TableType { get { return typeof(ProjectCostCategoryClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectCostCategoryClient)value; } }
        /*For Edit*/
        public CostCategoryPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata,true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CostCategoryPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }
        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api =leCostGroup.api= crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
                editrow =CreateNew() as ProjectCostCategoryClient;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
