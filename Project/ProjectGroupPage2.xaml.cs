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
    public partial class ProjectGroupPage2 : FormBasePage
    {
        ProjectGroupClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.ProjectGroupPage2; } }

        public override Type TableType { get { return typeof(ProjectGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectGroupClient)value; } }

        public ProjectGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit)
           : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (ProjectGroupClient)StreamingManager.Clone(sourcedata);
                editrow.Group = string.Empty;
                editrow.Name = string.Empty;
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }

        public ProjectGroupPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public ProjectGroupPage2(CrudAPI crudApi, string dummy)
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
            leWorkInProgress.api = leWorkInProgressOffset.api = leOutlay.api = leOutlayOffset.api = 
            leMaterials.api = leMaterialsOffset.api = leAutoNumber.api= leAdjustment.api=
            leAdjustmentOffset.api = leInvoiceAdjustment.api = leInvoiceAdjustmentOffset.api = crudapi;
            cbUseCostInWIP.ItemsSource = AppEnums.WIPValue.Values;

            if (editrow == null && LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as ProjectGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_BaseActions;
        }
    }
}

