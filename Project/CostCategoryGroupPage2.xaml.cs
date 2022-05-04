using Uniconta.API.System;
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

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CostCategoryGroupPage2 : FormBasePage
    {
        ProjectCostCategoryGroupClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.CostCategoryGroupPage2.ToString(); } }
        public override Type TableType { get { return typeof(ProjectCostCategoryGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectCostCategoryGroupClient)value; } }
        public CostCategoryGroupPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CostCategoryGroupPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;   
            lookupCostAccount.api=crudapi;
            lookupOffsetCostAccount.api=crudapi;
            lookupSummeryRevenue1.api = crudapi;
			lookupSummeryRevenue2.api = crudapi;
			lookupSummeryRevenue3.api = crudapi;
			lookupSummeryRevenue.api = crudapi;			
			leSalesVat.api = crudapi;
			leSalesVat1.api = crudapi;
			leSalesVat2.api = crudapi;
			leSalesVat3.api = crudapi;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as ProjectCostCategoryGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}


