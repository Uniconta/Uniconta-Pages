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
using Uniconta.API.Crm;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class PartnerProspectFollowUpPage2 : FormBasePage
    {
        PartnerProspectFollowUpClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.PartnerProspectFollowUpPage2.ToString(); } }
        public override Type TableType { get { return typeof(PartnerProspectFollowUpClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (PartnerProspectFollowUpClient)value; } }
        UnicontaBaseEntity master;
        bool isCopiedRow = false;
        public PartnerProspectFollowUpPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            this.master = master;
            isCopiedRow = !isEdit; 
            InitPage(api);
        }

        public PartnerProspectFollowUpPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(deCreated, deCreated);
#endif
        }

        public PartnerProspectFollowUpPage2(CrudAPI crudApi, UnicontaBaseEntity sourceData) : base(crudApi, null)
        {
            InitializeComponent();
            InitPage(crudApi);
        }

        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            deCreated.IsReadOnly = true;
            deCreated.AllowDefaultButton = false;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as PartnerProspectFollowUpClient;
                    editrow.SetMaster(master);
                }
                deCreated.IsReadOnly = false;
                deCreated.AllowDefaultButton = true;
                liUpdatedAt.Visibility = Visibility.Collapsed;
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
