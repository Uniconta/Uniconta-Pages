using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
   
    public partial class CrmCampaignPage2 : FormBasePage
    {
        CrmCampaignClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.CrmCampaignPage2.ToString(); } }

        public override Type TableType { get { return typeof(CrmCampaignClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CrmCampaignClient)value; } }

        public CrmCampaignPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CrmCampaignPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtCreated, txtCreated);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            var Comp = api.CompanyEntity;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as CrmCampaignClient;
            }
            leGroup.api = leEmployee.api = crudapi;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
